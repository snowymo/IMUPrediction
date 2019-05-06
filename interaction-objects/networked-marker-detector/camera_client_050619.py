import socket
import cv2
import pickle
import math
import sys
import numpy as np
import time

#-----------------------------------------------------------------------------------
# SETUP SOCKETS

cap = cv2.VideoCapture(0)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
print("FPS:",cap.get(cv2.CAP_PROP_FPS))
print("RES:",cap.get(cv2.CAP_PROP_FRAME_WIDTH),cap.get(cv2.CAP_PROP_FRAME_HEIGHT))

TCP_IP_ADDRESS = "127.0.0.1"                                # LAN IP ADDRESS OF SERVER
TCP_PORT_NO = 20375
# ADDRESS = (TCP_IP_ADDRESS, PORT_NO)

clientsocket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
clientsocket.connect((TCP_IP_ADDRESS, TCP_PORT_NO))

#-----------------------------------------------------------------------------------
# SEND VIDEO FRAMES FROM CV2 CAPTURE TO SERVER

frame_number = 1
t = 0.0
while(True):
        # Capture frame-by-frame
        ret,frame=cap.read()
        if frame_number % 1 == 0:   #LIMIT FRAMERATE BY ONLY PASSING CERTAIN FRAMES TO SEND, INDENT BLOCK OF CODE BENEATH HERE
                grayscale = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
                flattened = grayscale.flatten()
                hsize = 14              # Bytes
                dsize = flattened.shape[0]      # Bytes
                frame_w = grayscale.shape[1]
                frame_h = grayscale.shape[0]
                t = time.time()
                clientsocket.send(flattened)
        dt = time.time() - t
        print(dt)
        print("frame #: ",frame_number)
        frame_number += 1

cap.release()
#-----------------------------------------------------------------------------------