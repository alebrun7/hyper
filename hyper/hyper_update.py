import urllib.request
import json
import os
import sys
import subprocess
import signal
import shutil
import time

def remove_temp():
    print("remove temp")
    if os.path.exists("./publishlinux-arm.tar.xz"):
        os.unlink("./publishlinux-arm.tar.xz")
    if os.path.exists("./publishlinux-arm"):
        shutil.rmtree("./publishlinux-arm")
    if os.path.exists("./logs"):
        shutil.rmtree("./logs")
    if os.path.exists('./events.db'):
        os.unlink('./events.db')
    print("done")

def dlProgress(count, blockSize, totalSize):
    percent = int(count*blockSize*100/totalSize)
    sys.stdout.write("\rdownloading...%d%%" % percent)
    sys.stdout.flush()

def make_executable(path):
    mode = os.stat(path).st_mode
    mode |= (mode & 0o444) >> 2    # copy R bits to X
    os.chmod(path, mode)

def remove_inhausUDP_cronjob():
    if os.path.exists("current.cron"):
        os.unlink("current.cron")
    if os.path.exists("new.cron"):
        os.unlink("new.cron")

    with open("current.cron", "w") as cron:
        subprocess.call("crontab -l".split(" "), stdout=cron)

    with open("current.cron", "r") as org:
        with open("new.cron", "w") as new:
            for line in org:
                if "watchdog_zwave.sh" in line and not line.startswith("#"):
                    new.write("#" + line)
                else:
                    new.write(line)

    subprocess.call("crontab ./new.cron".split(" "))


    if os.path.exists("current.cron"):
        os.unlink("current.cron")
    if os.path.exists("new.cron"):
        os.unlink("new.cron")

def check_current_directory():
    if os.getcwd() == hyper_path:
        print("Current directory is the production directory, use a working directory for update!")
        sys.exit(1)
        
hyper_path = "/var/inhaus/hyper"
hyper_version_path = hyper_path + "/version.txt"
hyper_version_remote_url = 'https://api.github.com/repos/alebrun7/hyper/releases/latest'
hyper_latest_url = 'https://github.com/alebrun7/hyper/releases/latest/download/publishlinux-arm.tar.xz'
default_com = "/dev/ttyUSB_ZStickGen5"

if len(sys.argv) > 1:
    default_com = sys.argv[1]

check_current_directory()

#get remote version
res = urllib.request.urlopen(hyper_version_remote_url)
res_body = res.read()
j = json.loads(res_body.decode("utf-8"))
remote_version = j["tag_name"]
print("remote version: " + remote_version)

#get local version
local_version = "N/A"
if os.path.exists(hyper_version_path):
        with open(hyper_version_path, 'r') as f:
                local_version = f.read()
print("local version: " + local_version)

print("u sure? (y/n)")
sure = input()
if sure != "y":
    print("ok bye")
    sys.exit(0)

remove_temp()

#download latest release
print("downloading latest version")
urllib.request.urlretrieve(hyper_latest_url, 'publishlinux-arm.tar.xz', reporthook=dlProgress)
print("\ndone!")

#extract
print("extracting")
subprocess.call('tar xf publishlinux-arm.tar.xz'.split(' '))
print("done!")

# remove cronjob
remove_inhausUDP_cronjob()

#stopping and removing inhHausZwave
print("removing inHausUDPzwave")
if os.path.exists("/etc/init.d/inHausUDPzwave"):
    subprocess.call("/etc/init.d/inHausUDPzwave stop".split(" "))
    os.unlink("/etc/init.d/inHausUDPzwave")
print("done")

#stop hyper
print("stopping hyper")
if os.path.exists("/etc/init.d/hyper"):
    subprocess.call("/etc/init.d/hyper stop".split(" "))
    os.unlink("/etc/init.d/hyper")
shutil.copyfile("./publishlinux-arm/hyperInitD", "/etc/init.d/hyper")
make_executable("/etc/init.d/hyper")
subprocess.call("update-rc.d hyper defaults".split(" "))
print("done")


print("update udev")
if os.path.exists("/etc/udev/rules.d/20_ZStickGen5.rules"):
    os.unlink("/etc/udev/rules.d/20_ZStickGen5.rules")
if os.path.exists("/etc/udev/rules.d/20_ZwaveUSBStick.rules"):
    os.unlink("/etc/udev/rules.d/20_ZwaveUSBStick.rules")
shutil.copyfile("./publishlinux-arm/20_ZStickGen5.rules", "/etc/udev/rules.d/20_ZStickGen5.rules")
print("done")

#backup logs and events
print("backup")
if os.path.exists('/var/inhaus/hyper/logs'):
    shutil.copytree('/var/inhaus/hyper/logs', './logs')
if os.path.exists('/var/inhaus/hyper/events.db'):
    shutil.copyfile('/var/inhaus/hyper/events.db', './events.db')
if os.path.exists('/var/inhaus/hyper/config.yaml'):
    shutil.copyfile('/var/inhaus/hyper/config.yaml', './config_bak.yaml')

print("done")

#delete hyper folder
print("delete old")
if os.path.exists('/var/inhaus/hyper'):
    shutil.rmtree('/var/inhaus/hyper', True)
print("done")

#copy downloaded hyper folder and backups
print("copy new")
shutil.copytree('./publishlinux-arm', '/var/inhaus/hyper')
if os.path.exists('./logs'):
    shutil.copytree('./logs', '/var/inhaus/hyper/logs')
if os.path.exists('./events.db'):
    shutil.copyfile('./events.db', '/var/inhaus/hyper/events.db')
else:
    open('/var/inhaus/hyper/events.db', 'a').close()
print("done")

remove_temp()

#make executable
make_executable('/var/inhaus/hyper/hyper')

#print("starting hyper in background")
#subprocess.call("/etc/init.d/hyper start".split(" "))
print("reload udev")
subprocess.call("udevadm control --reload-rules".split(" "))
subprocess.call("udevadm trigger".split(" "))
#subprocess.Popen(['nohup', './hyper', default_com], stdout=open('/dev/null', 'w'), stderr=open('logfile.log', 'a'), preexec_fn=os.setpgrp, cwd="/var/inhaus/hyper")
time.sleep(5)
print("done")

print("starting client to verify")
subprocess.call("./ClientTCP", cwd="/var/inhaus/hyper")
