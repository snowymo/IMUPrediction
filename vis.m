% vis the quaternion or euler angle from node js
dr = fusiondemo.drawRotation;

fileID = fopen('forMatlab.txt','r');
formatSpec = '%f %f %f %f %f %f %f %f';
sizeA = [8 Inf];
A = fscanf(fileID,formatSpec,sizeA);

figure;
euld = A(3:5);
%dr.drawEulerRotation(gca, euld);
dr.drawGlobalAxes(gca, {'x_{parent}', ...
    'y_{parent}', 'z_{parent}'});
dr.drawRotatedAxesByEuler(gca, euld, ...
    {'x_{child}', 'y_{child}', 'z_{child}'}, 0.9);
euld = A(3:5,2);
%dr.drawEulerRotation(gca, euld);
dr.drawRotatedAxesByEuler(gca, euld,{'x_{child}', 'y_{child}', 'z_{child}'}, 0.9);

