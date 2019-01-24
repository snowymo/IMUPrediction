% vis each axis for euler angles

fileID = fopen('forMatlab.txt','r');
formatSpec = '%f %f %f %f %f %f %f %f';
sizeA = [8 Inf];
A = fscanf(fileID,formatSpec,sizeA);

figure
time = A(2,:);
predx = A(3,:);
realx = A(6,:);
predf = plot(time,predx,'-+');
predf.Color = "red";

hold on
realf = plot(time,realx,'-o');
realf.Color = "green";
hold off

xlabel('time(s)')
ylabel('predicted euler angle x(degree)')
title('Plot of the Euler Angle Prediction for X axis')