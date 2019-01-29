% vis each axis for euler angles

fileID = fopen('forMatlab.txt','r');
formatSpec = '%f %f %f %f %f %f %f %f';
sizeA = [8 Inf];
A = fscanf(fileID,formatSpec,sizeA);

figure
time = A(2,:);
predx = A(3,:);
realx = A(6,:);
predy = A(4,:);
realy = A(7,:);
predz = A(5,:);
realz = A(8,:);
predf = plot(time,predx,'-+');
predf.Color = "red";

hold on
realf = plot(time,realx,'-o');
realf.Color = "green";

hold on
realf = plot(time,predy,'-o');
realf.Color = "red";

hold on
realf = plot(time,realy,'-o');
realf.Color = "green";

hold on
realf = plot(time,predz,'-o');
realf.Color = "red";

hold on
realf = plot(time,realz,'-o');
realf.Color = "green";
hold off

xlabel('time(s)')
ylabel('predicted euler angle x(degree)')
title('Plot of the Euler Angle Prediction for three axis')