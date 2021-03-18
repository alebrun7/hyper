import serial
import sys
import glob
import os
import threading
import tty
import datetime
import socket
import argparse
import random
import fcntl
import select
import logging
from time import sleep

logging.basicConfig(filename='/opt/multiplexer.log',level=logging.DEBUG)

def serial_ports():
    if sys.platform.startswith('win'):
        ports = ['COM%s' % (i + 1) for i in range(256)]
    elif sys.platform.startswith('linux') or sys.platform.startswith('cygwin'):
        # this excludes your current terminal "/dev/tty"
        ports = glob.glob('/dev/tty[A-Za-z]*')
    elif sys.platform.startswith('darwin'):
        ports = glob.glob('/dev/tty.*')
    else:
        raise EnvironmentError('Unsupported platform')

    result = []
    for port in ports:
        try:
            s = serial.Serial(port)
            s.close()
            result.append(port)
        except (OSError, serial.SerialException):
            pass
    return result

if __name__ == '__main__':
    logging.info('Available Serial Ports are: ' + str(serial_ports()))

shut_down_flag = False

Multiplexer_UDP_IP= '127.0.0.1'
Multiplexer_UDP_Port= 3001

Hyper_UDP_IP= '127.0.0.1'
Hyper_UDP_Port= 4123


udp_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

udp_socket.bind((Multiplexer_UDP_IP,Multiplexer_UDP_Port))

### Helper
def print_info(text):
    now = datetime.datetime.now()
    logging.info( '[' + now.strftime("%Y-%m-%d %H:%M:%S") + ']:\t' + text)
    
def print_trace(text):
    global mode_quiet
    if mode_quiet:
        return
    now = datetime.datetime.now()
    logging.error( '[' + now.strftime("%Y-%m-%d %H:%M:%S") + ']:\t' + text)
    
def print_debug(text):
    now = datetime.datetime.now()
    logging.debug('[' + now.strftime("%Y-%m-%d %H:%M:%S") + ']:\t' +  text)
    
def print_warn(text):
    now = datetime.datetime.now()
    logging.warning( '[' + now.strftime("%Y-%m-%d %H:%M:%S") + ']:\t' +  text)
    
    
def init(client_count, fake_bus_prefix):
    (fake_bus_prefix,client_count)
    
###    

def create_bus(index):
    mfd, sfd = os.openpty()
    tty.setraw(mfd)
    tty.setraw(sfd)
    print_debug('use %s' % os.ttyname(sfd))
    print_debug('creating symlink')
    fake_name = fake_bus_prefix + str(index)
    if os.path.exists(fake_name):
        print_warn('remove old symlink')
        os.remove(fake_name)
    os.symlink(os.ttyname(sfd), fake_name)
    master = {"index": index, "fd": mfd, "bus": fake_name}
    slave = {"index": index, "fd": sfd, "bus": fake_name}
    print_info('created client_tty %s' % fake_name)
    return master,slave

def target_to_clients(udp_socket, clients):
    global shut_down_flag
    while True:
        try:
            data, addressInfo = udp_socket.recvfrom(100)
            x = data
            try:
              write_to_clients(clients, x)
            except Exception as e:
              print_warn(e)
        except Exception as e:
            print_warn(e)
            print_warn(e)
            shut_down_flag = True
            return
            
def client_to_target(client, udp_socket):
    global shut_down_flag
    client_index = client["index"] + 1 
    while True:
        try:
            x = os.read(client["fd"],1)
            try:
              write_to_target(x)
            except Exception as e:
              print_warn(e)
        except Exception as e:
            print_warn('stopped reading client %udp_socket' % client["bus"])
            print_warn(e)
            shut_down_flag = True
            return            
            
def write_to_clients(clients, text):
    for client in clients:
        os.write(client["fd"], text)
        
def write_to_target(x):
    udp_socket.sendto(x,(Hyper_UDP_IP,Hyper_UDP_Port))

parser = argparse.ArgumentParser()
parser.add_argument("-c", "--client_count", help="number of fake ports (default=1)" ,type=int, default=2, metavar='N')
parser.add_argument("-f", "--fake_bus_name", help="name of fake bus prefix (default=/dev/ttyS0fake)", default='/dev/ttyS0fake')
parser.add_argument("-q", "--quiet", help="no debug output", action='store_true')

args = parser.parse_args()
client_count = args.client_count
fake_bus_prefix = args.fake_bus_name
mode_quiet = args.quiet

client_masters = []
client_slaves = []
init( client_count, fake_bus_prefix)
write_lock = threading.Lock()

print_info("creating %d clients:" % client_count)
for index in range(client_count):
    master, slave = create_bus(index)
    client_masters.append(master)
    client_slaves.append(slave)
print_info("all clients created")

threads = []

thread1 = threading.Thread(target=target_to_clients, args = (udp_socket,client_masters,))
thread1.daemon = True
thread1.start()
threads.append(thread1)

for client in client_masters:
    thread = threading.Thread(target=client_to_target, args = (client,udp_socket))
    thread.daemon = True
    thread.start()
    threads.append(thread)

try:
    while True:
        sleep(0.5)
        if shut_down_flag:
            raise
except:
    logging.info('shutting down..')
    print_info('closing ports...')
    for master in client_masters:
        os.close(master["fd"])
    for client in client_slaves:
        os.close(client["fd"])
    print_info('removing symlinks...')
    for client in client_slaves:
        os.unlink(client["bus"])
        print_warn('%s removed' % client["bus"])
   
    sys.exit(0)