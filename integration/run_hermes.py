"""Fail closed for non-local network connections made by the inference process.

MCP documentation fetches run in a separate process. Shell tools run in Docker.
"""
import ipaddress
import socket
import sys

def local_host(host):
    if host in ('localhost',None,''): return True
    try: return ipaddress.ip_address(host).is_loopback
    except ValueError: return False

def audit(event,args):
    if event=='socket.getaddrinfo' and not local_host(args[0]):
        raise PermissionError('Agent inference process may resolve localhost only; use MCP for documentation.')
    if event=='socket.connect':
        sock,address=args
        if sock.family!=socket.AF_UNIX and isinstance(address,tuple) and not local_host(address[0]):
            raise PermissionError('External network blocked in inference process; all AI must remain local.')

sys.addaudithook(audit)
if __name__=='__main__':
    from hermes_cli.main import main
    main()
