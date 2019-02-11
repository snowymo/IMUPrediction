% visualize each gyro pred
fileID = fopen('forMatlab.txt','r');
formatSpec = '%f %f %f %f %f %f %f %f';
sizeA = [17 Inf];
A = fscanf(fileID,formatSpec,sizeA);

l = length(A)-350

f1 = figure;
time = A(2,:);
predx = A(3,:);
realx = A(6,:);
% up - down
predy = A(4,:);
realy = A(7,:);
% left - right
predz = A(5,:);
realz = A(8,:);

m = A(9:17,:);

predf = plot(time(1:l),predx(1:l),'-+');
predf.Color = "red";

hold on
realf = plot(time(1:l),realx(1:l),'-+');
realf.Color = "green";

hold on
realf = plot(time(1:l),predy(1:l),'-o');
realf.Color = "red";

hold on
realf = plot(time(1:l),realy(1:l),'-o');
realf.Color = "green";

hold on
realf = plot(time(1:l),predz(1:l),'-.');
realf.Color = "red";

hold on
realf = plot(time(1:l),realz(1:l),'-.');
realf.Color = "green";
hold off

xlabel('time(ms)')
ylabel('predicted gyro(rad/s)')
title('Plot of the Gyro Prediction for three axes')

forward = [0; 0; 1];
f2 = figure;
forwards = zeros(length(time-1),3);
rotationCurrent = eye(3);
forwardsGT= zeros(length(time-1),3);
rotationCurrentGT = eye(3);
for i = 2:length(time)
        curT = time(i);
        curX = predx(i);
        curY = predy(i);
        curZ = predz(i);
        % Calculate the angular speed of the sample
        omegaMagnitude = sqrt(curX * curX + curY * curY + curZ * curZ);
        
        % Integrate around this axis with the angular speed by the timestep
        % in order to get a delta rotation from this sample over the timestep
        % We will convert this axis-angle representation of the delta rotation
        % into a quaternion before turning it into the rotation matrix.
        dT = (curT - time(i-1)) / 1000;
        thetaOverTwo = omegaMagnitude * dT / 2.0;
        sinThetaOverTwo = sin(thetaOverTwo);
        cosThetaOverTwo = cos(thetaOverTwo);
        deltaRotationVector = [cosThetaOverTwo sinThetaOverTwo*curX sinThetaOverTwo*curY sinThetaOverTwo*curZ];
        % User code should concatenate the delta rotation we computed with the current rotation
        % in order to get the updated rotation.
        curRotation = quat2rotm(deltaRotationVector);
        rotationCurrent = curRotation * rotationCurrent;
        
        forwards(i-1,:) = rotationCurrent * forward; % result vector3
        %%%
        curX = realx(i);
        curY = realy(i);
        curZ = realz(i);
        % Calculate the angular speed of the sample
        omegaMagnitude = sqrt(curX * curX + curY * curY + curZ * curZ);
        
        % Integrate around this axis with the angular speed by the timestep
        % in order to get a delta rotation from this sample over the timestep
        % We will convert this axis-angle representation of the delta rotation
        % into a quaternion before turning it into the rotation matrix.
        %dT = (curT - time(i-1)) / 1000;
        thetaOverTwo = omegaMagnitude * dT / 2.0;
        sinThetaOverTwo = sin(thetaOverTwo);
        cosThetaOverTwo = cos(thetaOverTwo);
        deltaRotationVector = [cosThetaOverTwo sinThetaOverTwo*curX sinThetaOverTwo*curY sinThetaOverTwo*curZ];
        % User code should concatenate the delta rotation we computed with the current rotation
        % in order to get the updated rotation.
        curRotation = quat2rotm(deltaRotationVector);
        rotationCurrentGT = curRotation * rotationCurrentGT;
        
        forwardsGT(i-1,:) = rotationCurrentGT * forward; % result vector3
end
plot3(forwards(1,1), forwards(1,2), forwards(1,3),'o');
hold on
plot3(0, 0, 0,'o');
hold on

p = plot3(forwards(1:l-1,1), forwards(1:l-1,2), forwards(1:l-1,3));
p.Color = "red";
hold on
l = length(forwardsGT)-350
p = plot3(forwardsGT(1:l-1,1), forwardsGT(1:l-1,2), forwardsGT(1:l-1,3));
p.Color = "green";
xlabel('time(ms)')
ylabel('ground truth forward')
title('Plot of the Gyro Prediction by Showing Forward Vector')

%hold on
%f3 = figure;
%forwards2 = zeros(length(time),3);
%for i = 1 : length(time)
%    mm = reshape(m(:,i),[3,3]);
%    forwards2(i,:) = mm * forward; % result vector3
%end
%p = plot3(forwards2(:,1), forwards2(:,2), forwards2(:,3));
%p.Color = "green";
%xlabel('time(ms)')
%ylabel('ground truth rotation')
%title('Plot of the Gyro Prediction by Showing Forward Vector')

hold off