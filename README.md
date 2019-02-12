# IMUPrediction

This project is investigating how to predict IMU for AR/VR devices. It includes IMU retrieval, prediction, and applying to final texture.

## IMU retrieval
Both iPhone and android are supported. 
### For android
Launch AndroidStudio and open project in root folder. 
### For iPhone
1. Pull the submodule, 
[git@github.com:snowymo/IOSSocket.git](https://github.com/snowymo/IOSSocket) and open *.xcworkspace.
2. Change the ip to HOST ip address
3. Click `send` to send IMU data

![iphone screenshot](https://github.com/snowymo/IMUPrediction/blob/master/images/iPhoneIMU.png "iphone screenshot")


## Prediction
1. Go to folder `unity`, choose the device in object `Receiver`.
2. Change the `HOST` based on the HOST ip address, which is the ip address of the machine running unity.
3. Change the device accordingly.

![unity receiver](https://github.com/snowymo/IMUPrediction/blob/master/images/unity-receiver.PNG "unity receiver")
