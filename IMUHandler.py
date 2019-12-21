# this class will viz acc and gyro
# calculate pos from acc, rvec from gyro
# viz rvec and gyro

import math
import argparse
import numpy as np
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
from matplotlib.widgets import Slider, Button

class IMUHandler:
    def __init__( self):
        self.acc = []
        self.linear_acc = []
        self.vec = []
        self.gyro = []
        self.pos = []
        self.rvec = []
        return

    def __init__ (self, acc, gyro, pos=[], rmatrix=[]):
        self.acc = acc
        self.gravity = []
        self.gyro = gyro
        self.linear_acc = []
        self.vec = []
        if len(pos) > 0:
            self.pos = pos
        else:
            self.pos = []
            self.calculate_pos()

        if len(rmatrix) > 0:
            self.rmatrix = rmatrix
        else:
            self.rmatrix = []
            self.rvec = []
            self.calculate_rvec()

        self.apply_filter()

        return

    def apply_filter(self):
        # alpha = t / (t + dT)
        gravity = None
        for i, val in enumerate(self.acc):
            if i > 0:
                if self.acc[i,0] != self.acc[i-1,0]:
                    alpha = self.acc[i,0] / (self.acc[i,0] + self.acc[i,0]-self.acc[i-1,0])
                    # Isolate the force of gravity with the low-pass filter.
                    cur_gravity = alpha * gravity + (1 - alpha) * val[1:]
                    # Remove the gravity contribution with the high-pass filter.
                    self.linear_acc.append([self.acc[i, 0]] + ((self.acc[i,1:] - cur_gravity)/1.0).tolist())
                    gravity = cur_gravity
            else:
                self.linear_acc.append([self.acc[i, 0]] + [0,0,0])
                gravity = val[1:]
        self.linear_acc = np.asarray(self.linear_acc)

    def calculate_pos(self):
        # calculate the position from accelerometer
        # self.acc is an array of acc with timestamp based
        # apply filter first
        cur_vec = None
        for i, val in enumerate(self.linear_acc):
            if i > 1:
                dt = (self.linear_acc[i, 0] - self.linear_acc[0, 0]) / 1000.0
                cur_vec = vec + dt * self.linear_acc[i - 1, 1:]
                cur_pos = self.pos[i-1][1:] + dt * cur_vec
                self.pos.append([self.linear_acc[i, 0]] + cur_pos.tolist())
            elif i > 0:
                 # we don't calculate vec for the first one
                dt = (self.linear_acc[i, 0] - self.linear_acc[0, 0]) / 1000.0
                cur_vec = vec + dt * self.linear_acc[i - 1, 1:]
                self.pos.append([self.linear_acc[i, 0]] + [0, 0, 0])
            else:
                cur_vec = [0, 0, 0]
                self.pos.append([self.linear_acc[i, 0]] + [0, 0, 0])
            vec = cur_vec
        self.pos = np.asarray(self.pos)

    def calculate_rvec(self):
        EPSILON = 0.1
        # calculate quat from gyro NS2S = 1.0f / 1000000000.0f
        for i, val in enumerate(self.gyro):
            if i > 0:
                if self.gyro[i, 0] != self.gyro[i - 1, 0]:
                    dt = (self.gyro[i,0]-self.gyro[i-1,0]) / 1000.0
                    # Axis of the rotation sample, not normalized yet.
                    # Calculate the angular speed of the sample
                    omegaMagnitude = math.sqrt(val[1] * val[1] + val[2] * val[2] + val[3] * val[3]);
                    # Normalize the rotation vector if it's big enough to get the axis (that is, EPSILON should represent your maximum allowable margin of error)
                    if omegaMagnitude > EPSILON:
                        val[1] /= omegaMagnitude
                        val[2] /= omegaMagnitude
                        val[3] /= omegaMagnitude
                        # Integrate around this axis with the angular speed by the timestep in order to get a delta rotation from this sample over the timestep
                        # We will convert this axis - angle representation of the delta rotation into a quaternion before turning it into the rotation matrix.
                    thetaOverTwo = omegaMagnitude * dt / 2.0
                    sinThetaOverTwo = math.sin(thetaOverTwo)
                    cosThetaOverTwo = math.cos(thetaOverTwo)
                    deltaRotationVector = [sinThetaOverTwo * val[1], sinThetaOverTwo * val[2], sinThetaOverTwo * val[3], cosThetaOverTwo]
                    self.rvec.append([self.gyro[i, 0]] + deltaRotationVector)
            else:
                self.rvec.append([self.gyro[i, 0]] + [0, 0, 0, 1])
        self.rvec = np.asarray(self.rvec)

def viz_time_axis(time, pts, ax, color, marker = '.', size = 10):
    ax.scatter(time, pts, c=color, marker=marker, s=size)

def viz_raw_data(points, ax, stime, color, marker = '.', size = 10):
    ax.scatter3D(points[:, 0], points[:, 1], points[:, 2],
                 color=color, marker = marker, s=size)
    ax.set_xlabel('X')
    ax.set_zlabel('Z')
    ax.set_ylabel('Y')

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('--imu', type=str, default="imu-z", help='imu file')
    parser.add_argument('--optitrack', type=str, default="optitrack-z", help='optitrack file')
    args = parser.parse_args()
    imu_data = np.loadtxt(args.imu + ".csv", delimiter=",")
    imu_handler = IMUHandler(imu_data[:,0:4],imu_data[:,[0,4,5,6]], imu_data[:,[0,7,8,9]],imu_data[:,[0,10,11,12,13,14,15,16,17,18]])

    optitrack = np.loadtxt(args.optitrack + ".csv")

    # visualize the pos and rvec
    fig = plt.figure()
    # plt.axis('scaled')
    subax = fig.add_subplot(221, projection='3d')
    subax.set_xlabel('X')
    # subax.set_xlim([0, 0.4])
    # subax.set_ylim([0, 0.4])
    # subax.set_zlim([0, 0.4])
    subax.set_zlabel('Z')
    subax.set_ylabel('Y')
    # subbx = fig.add_subplot(222, projection='3d')
    subbx = fig.add_subplot(222)
    subcx = fig.add_subplot(223)
    subdx = fig.add_subplot(224)
    # imu_handler.pos = imu_handler.pos[200:]
    imu_in_optitrack_axis = [imu_handler.pos[:, 1],imu_handler.pos[:, 3],-imu_handler.pos[:, 2]]
    imu_in_optitrack_axis = np.asarray(imu_in_optitrack_axis)
    imu_in_optitrack_axis = np.swapaxes(imu_in_optitrack_axis, 0, 1)
    subax.scatter3D(imu_handler.pos[:, 3], imu_handler.pos[:, 1], imu_handler.pos[:, 2], color="red")
    viz_time_axis(imu_handler.pos[:, 0]/1000.0, imu_handler.pos[:, 1], subbx, "red")
    viz_time_axis(imu_handler.pos[:, 0]/1000.0, imu_handler.pos[:, 3], subcx, "red")
    viz_time_axis(imu_handler.pos[:, 0]/1000.0, -imu_handler.pos[:, 2], subdx, "red")

    # viz_raw_data(optitrack[:, 1:4]-optitrack[0, 1:4], subax, None, "green")
    subax.scatter3D(optitrack[:, 1]-optitrack[0, 1], optitrack[:, 2]-optitrack[0, 2], optitrack[:, 3]-optitrack[0, 3], color="green")
    viz_time_axis(optitrack[:, 0], optitrack[:, 1]-optitrack[0, 1], subbx, "green")
    viz_time_axis(optitrack[:, 0] , optitrack[:, 2] - optitrack[0, 2], subcx, "green")
    viz_time_axis(optitrack[:, 0] , optitrack[:, 3] - optitrack[0, 3], subdx, "green")
    # viz_time_axis(optitrack[:, 0]*1000, optitrack[:, 2]-optitrack[0, 2], subcx, "green")
    # viz_time_axis(optitrack[:, 0]*1000, optitrack[:, 3]-optitrack[0, 3], subdx, "green")
    plt.show()
    # imu opt
    # x x
    # y z
    # z y
