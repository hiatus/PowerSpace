import sys
import socket
import select
import argparse


_BANNER = '''\
PowerSpaceServer [options] [port]
    -h, --help             this
    -v, --verbose          enable runtime messages
    -b, --bind    [addr]   bind socket to [addr] instead of listening on all interfaces
    -k, --key     [hex]    enable RC4 encryption by specifying a key
'''

_DEFAULT_BIND = '0.0.0.0'


class RC4Context(object):
    SKIP = 4096

    def __init__(self, key: bytes):
        if not isinstance(key, bytes):
            raise ValueError(f'{type(key).__str__} where bytes was expected')

        j = 0
        key_len = len(key)

        self.i = 0
        self.j = 0
        self.state = bytearray(i for i in range(256))

        for i in range(256):
            j = (j + i + key[i % key_len]) % 256

            aux = self.state[i]
            self.state[i] = self.state[j]
            self.state[j] = aux

        self.skip(self.SKIP)

    def skip(self, n: int):
        for _ in range(n):
            self.i = (self.i + 1) % 256
            self.j = (self.j + self.state[self.i]) % 256

            aux = self.state[self.i]
            self.state[self.i] = self.state[self.j]
            self.state[self.j] = aux

    def encrypt(self, data: bytes):
        data = bytearray(data)

        for i in range(len(data)):
            self.i = (self.i + 1) % 256
            self.j = (self.j + self.state[self.i]) % 256

            aux = self.state[self.i]
            self.state[self.i] = self.state[self.j]
            self.state[self.j] = aux

            data[i] ^= self.state[(self.state[self.i] + self.state[self.j]) % 256]


        return bytes(data)

    def decrypt(self, data: bytearray):
        return self.encrypt(data)


class Connection:
    MAX_WAIT = 0.1
    MAX_RECV = 32768

    def __init__(self, bind_host: str, bind_port: str, rc4_key: bytes):
        self.srv = None
        self.cli = None

        self.bind_host = bind_host
        self.bind_port = bind_port
        self.crypter = RC4Context(rc4_key) if rc4_key else None

        try:
            self.srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

            self.srv.bind((self.bind_host, self.bind_port))
            self.srv.listen(1)

            self.cli, self.cli_info = self.srv.accept()
        except Exception as e:
            self.close()
            raise e

    def recv(self) -> bytes:
        data = self.cli.recv(self.MAX_RECV)

        while True:
            rlist, _, _ = select.select([self.cli], [], [], self.MAX_WAIT)

            if not rlist:
                break

            data += self.cli.recv(self.MAX_RECV, socket.MSG_DONTWAIT)

        if self.crypter is not None:
            data = self.crypter.decrypt(data)

        return data

    def send(self, data: bytes):
        if self.crypter is not None:
            data = self.crypter.encrypt(data)

        self.cli.sendall(data)

    def close(self):
        if self.srv is not None:
            self.srv.close()

        if self.cli is not None:
            self.cli.close()


def clear():
    print('\033c\033[3J', end='')


def parse_args():
    if len(sys.argv) < 2:
        print(_BANNER)
        sys.exit(1)

    parser = argparse.ArgumentParser(
        usage=_BANNER, description='PowerSpaceServer', add_help=False
    )

    parser.add_argument('-h', '--help', action='store_true')
    parser.add_argument('-v', '--verbose', action='store_true')
    parser.add_argument('-b', '--bind', type=str, default=None)
    parser.add_argument('-k', '--key', type=str, default=None)
    parser.add_argument('port', type=int)

    args = parser.parse_args()

    if args.help:
        print(_BANNER)
        sys.exit(0)

    if not args.bind:
        args.bind = _DEFAULT_BIND
        
    if args.key:
        args.key = bytes.fromhex(args.key)

    if args.port < 1 or args.port > 65535:
        print(f'[!] Invalid [port]: {args.port}')

    return args


if __name__ == '__main__':
    args = parse_args()
    connection = None

    try:
        if args.verbose:
            print(f'[PowerSpaceServer] Listening on {args.bind}:{args.port}')

        connection = Connection(args.bind, args.port, args.key)

        if args.verbose:
            print(
                '[PowerSpaceServer] Connection from '
                f'{connection.cli_info[0]}:{connection.cli_info[1]}'
            )

        output = connection.recv().decode('latin-1')
        prompt = output.split('\n')[-1]
        output = '\n'.join(output.split('\n')[:-1])

        while True:
            if output != prompt:
                print(output)

            while not (cmd := input(prompt).strip()):
                output = prompt
                continue

            if cmd in ('clear', 'cls'):
                clear()
                output = prompt

                continue

            connection.send(cmd.encode())

            if cmd == 'exit':
                break

            output = connection.recv().decode('latin-1')
            prompt = output.split('\n')[-1]
            output = '\n'.join(output.split('\n')[:-1])

    except UnicodeDecodeError as e:
        print(f'\n[!] Cryptographic asynchrony detected')
        connection.close()
        raise e

    except Exception as e:
        print(f'\n[!] {type(e).__name__}: {e}')

    except KeyboardInterrupt as ki:
        print('\n[!] KeyboardInterrupt received')

    if connection is not None:
        if args.verbose:
            print('[PowerSpaceServer] Closing connection')

        connection.close()