% visualize a vec representing the quaternion
fileID = fopen('forMatlab.txt','r');
formatSpec = '%f %f %f %f %f %f %f %f';
sizeA = [8 Inf];
A = fscanf(fileID,formatSpec,sizeA);

figure
predx = A(3,:);
predy = A(4,:);
predz = A(5,:);
rotm = eul2rotm([predx.' predy.' predz.'],'ZYX');
pred = zeros(3, length(predx));
for i = 1:length(predx)
    pred(:,i) = rotm(:,:,i)*[0;0;1];
end
pPredict = plot3(pred(1,:), pred(2,:), pred(3,:));
pPredict.Color = "red";

hold on
realx = A(6,:);
realy = A(7,:);
realz = A(8,:);
rotm = eul2rotm([realx.' realy.' realz.'],'ZYX');
real = zeros(3, length(predx));
for i = 1:length(predx)
    real(:,i) = rotm(:,:,i)*[0;0;1];
end
pReal = plot3(real(1,:), real(2,:), real(3,:));
pReal.Color = "green";
hold off