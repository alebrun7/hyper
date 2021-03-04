import serial
import sys
import glob
import os
import threading
import tty
import datetime
import socket

from time import sleep
import argparse
import random
import fcntl
import select


def serial_ports():
    """ Lists serial port names

        :raises EnvironmentError:
            On unsupported or unknown platforms
        :returns:
            A list of the serial ports available on the system
    """
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
    print('Available Serial Ports are: ' + str(serial_ports()))





shut_down_flag = False
z_wave_commands = {
    'ACK' :  b'\x06',
    'NAK' : b'\x15',
    'CAN' : b'\x18',
    'SOF' : b'\x01'
}



Multiplexer_UDP_IP= '127.0.0.1'
Multiplexer_UDP_IP_UDP_Port= 3001

Hyper_UDP_IP= '127.0.0.1'
Hyper_UDP_Port= 4123


s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

s.bind((Multiplexer_UDP_IP,Multiplexer_UDP_IP_UDP_Port))

### Helper
def print_info(text):
    now = datetime.datetime.now()
    print( '[' + now.strftime("%Y-%m-%d %H:%M:%S") + ']:\t' + text)
    
def print_trace(text):
    global mode_quiet
    if mode_quiet:
        return
    now = datetime.datetime.now()
    print( '[' + now.strftime("%Y-%m-%d %H:%M:%S") + ']:\t' + text)
    
def print_debug(text):
    now = datetime.datetime.now()
    print('[' + now.strftime("%Y-%m-%d %H:%M:%S") + ']:\t' +  text)
    
def print_warn(text):
    now = datetime.datetime.now()
    print( '[' + now.strftime("%Y-%m-%d %H:%M:%S") + ']:\t' +  text)
    
    
def init(target_tty, client_count, fake_bus_prefix):

    print('\tprefix for fake ttys:\t%s\r\n\ttarget:\t\t\t%s\r\n\tclients:\t\t%d' % 
    (fake_bus_prefix, target_tty, client_count))
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

def target_to_clients(target_tty_serial, clients):
    global shut_down_flag
    #print_debug("reading target %s" % target_tty_serial.name)
    while True:
        try:
            x = target_tty_serial.read(1)
            
            #if x == z_wave_commands['ACK']:
            #    print_trace('%s: ACK RECEIVED!' % target_tty_serial.name)
            #elif x == z_wave_commands['NAK']:
            #    print_trace('%s: NAK RECEIVED!' % target_tty_serial.name)
            #elif x == z_wave_commands['CAN']:
            #    print_trace('%s: CAN RECEIVED!' % target_tty_serial.name)
            if x == z_wave_commands['SOF']:
                #print_trace('%s: SOF RECEIEVED!' % target_tty_serial.name)
                n = target_tty_serial.read(1)
                n_int = int.from_bytes(n, byteorder='little')
                r = target_tty_serial.read(n_int)
                x = x + n + r
            #else:
             #   print('dunno...')
            #    print(hex(x[0]))
             #   continue

            write_to_clients(clients, x)
        except Exception as e:
            print_warn('stopped reading target')
            print(e)
            shut_down_flag = True
            return

def client_to_target(client, target_tty_serial):
    global shut_down_flag
    client_index = client["index"] + 1 
    print_debug('reading client %s' % client["bus"])
    while True:
        try:
            x = os.read(client["fd"],1)
            
            #if x == z_wave_commands['ACK']:
            #    print_trace('%s: ACK RECEIVED!' % client["bus"])
            #elif x == z_wave_commands['NAK']:
            #    print_trace('%s: NAK RECEIVED!' % client["bus"])
            #elif x == z_wave_commands['CAN']:
            #    print_trace('%s: CAN RECEIVED!' % client["bus"])
            if x == z_wave_commands['SOF']:
            #    print_trace('%s: SOF RECEIEVED!' % client["bus"])
                n = os.read(client["fd"],1)
                n_int = int.from_bytes(n, byteorder='little')
                r = os.read(client["fd"],n_int)
                x = x + n + r
            #else:
            #    print('dunno...')
            #    print(hex(x[0]))
            #    continue

            print_trace("reading %d bytes from client %s" % (len(x), client["bus"]))
            write_to_target(target_tty_serial,x)
            
              
        except Exception as e:
            print_warn('stopped reading client %s' % client["bus"])
            print(e)
            shut_down_flag = True
            return            
            

def udpclient_to_target(s, target_tty_serial):
    global shut_down_flag
    #client_index = client["index"] + 1 
    print_debug('reading udpclient')
    while True:
        try:
            print('Ready to receive by UDP')
            data, addressInfo = s.recvfrom(100)
            x = data
            
            #if x == z_wave_commands['ACK']:
            #    print_trace('%s: ACK RECEIVED!' % client["bus"])
            #elif x == z_wave_commands['NAK']:
            #    print_trace('%s: NAK RECEIVED!' % client["bus"])
            #elif x == z_wave_commands['CAN']:
            #    print_trace('%s: CAN RECEIVED!' % client["bus"])
            #if x == z_wave_commands['SOF']:
            #    print_trace('%s: SOF RECEIEVED!' % client["bus"])
                #n = os.read(client["fd"],1)
                #n_int = int.from_bytes(n, byteorder='little')
                #r = os.read(client["fd"],n_int)
                #x = x + n + r
            #else:
            #    print('dunno...')
            #    print(hex(x[0]))
            #    continue

            print_trace("reading %d bytes from client %s" % (len(x), "UDP"))
            write_to_target(target_tty_serial,x)
           
              
        except Exception as e:
            print_warn('stopped reading client %s' % "UDP")
            print(e)
            shut_down_flag = True
            return       


def write_to_target(target_tty_serial, text):
    #global write_lock
    #write_lock.acquire()
    print_trace('write %d bytes to %s...' % (len(text), target_tty))
    target_tty_serial.write(text)
    #write_lock.release()


def write_to_clients(clients, text):
    print_trace('write %d bytes to %d clients' %(len(text), len(clients)))
    for client in clients:
        os.write(client["fd"], text)

    try:
        print('Ready to send to UDP')
        s.sendto(
        text,
        (Hyper_UDP_IP,Hyper_UDP_Port)
        )
    except Exception as e:
        print('Problem with UDP')


parser = argparse.ArgumentParser()
parser.add_argument("-c", "--client_count", help="number of fake ports (default=2)" ,type=int, default=2, metavar='N')
parser.add_argument("-t", "--target_device", help="target serial port (default=/dev/ttyAMA0)", default='/dev/ttyAMA0')
parser.add_argument("-f", "--fake_bus_name", help="name of fake bus prefix (default=/dev/ttyS0fake)", default='/dev/ttyS0fake')
parser.add_argument("-q", "--quiet", help="no debug output", action='store_true')

args = parser.parse_args()
client_count = args.client_count
target_tty = args.target_device
fake_bus_prefix = args.fake_bus_name
mode_quiet = args.quiet


target_tty_serial = serial.Serial(target_tty, baudrate=115200)        
client_masters = []
client_slaves = []
init(target_tty, client_count, fake_bus_prefix)
write_lock = threading.Lock()

print_info("creating %d clients:" % client_count)
for index in range(client_count):
    master, slave = create_bus(index)
    client_masters.append(master)
    client_slaves.append(slave)
print_info("all clients created")

threads = []

thread1 = threading.Thread(target=target_to_clients, args = (target_tty_serial,client_masters,))
thread1.daemon = True
thread1.start()
threads.append(thread1)

for client in client_masters:
    thread = threading.Thread(target=client_to_target, args = (client,target_tty_serial,))
    thread.daemon = True
    thread.start()
    threads.append(thread)

#udp client thread
thread = threading.Thread(target=udpclient_to_target, args = (s,target_tty_serial,))
thread.daemon = True
thread.start()
threads.append(thread)
    

try:
    while True:
        sleep(0.5)
        if shut_down_flag:
            raise
except:
    print('')
    print('shutting down..')
    print_info('closing ports...')
    for master in client_masters:
        os.close(master["fd"])
    for client in client_slaves:
        os.close(client["fd"])
    target_tty_serial.close()
    print_info('removing symlinks...')
    for client in client_slaves:
        os.unlink(client["bus"])
        print_warn('%s removed' % client["bus"])
   
    sys.exit(0)