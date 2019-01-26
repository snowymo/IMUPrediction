//require('log-timestamp');
var regression = require('regression');

var PORT = 12345;
var HOST = '216.165.71.223';

var dgram = require('dgram');
var server = dgram.createSocket('udp4');
var Quaternion = require('quaternion');

var q = new Quaternion(1,0,0,0);

var rot_history = [], euler_history = [], gyro_history = [];
var history_length = 15;//200 samples/s * 0.03s
// how far ahead we are trying to predict, in seconds
var lag = 0.030;
var predictions = [], predictsEuler = [], predGyroList = [];

// take ip as the input
var argv = require('minimist')(process.argv.slice(2));
if('ip' in argv)
	HOST = argv['ip'];
//console.log(argv);
//console.log("host:" + HOST);

var THREE = require('three');

var last_time = 0;

// returns result of polynomial function
var poly = function (coeffs, x) {
    var res = 0;
    for (var i = 0; i < coeffs.length; i++) {
        res += coeffs[i] * Math.pow(x, (coeffs.length - i - 1));
    }
    return res;
}

// multiplies two quaternions
var mulquat = function (q1, q2) {
    var ret = [0, 0, 0, 1];
    ret[0] = q2[3] * q1[0] + q2[0] * q1[3] - q2[1] * q1[2] + q2[2] * q1[1];
    ret[1] = q2[3] * q1[1] + q2[0] * q1[2] + q2[1] * q1[3] - q2[2] * q1[0];
    ret[2] = q2[3] * q1[2] - q2[0] * q1[1] + q2[1] * q1[0] + q2[2] * q1[3];
    ret[3] = q2[3] * q1[3] - q2[0] * q1[0] - q2[1] * q1[1] - q2[2] * q1[2];
    return ret;
}

// returns conjugate, or inverse, of a quaternion
var conj = function (q) {
    return [-q[0], -q[1], -q[2], q[3]];
}

var clamp = function (x, min, max) {
    return Math.min(Math.max(x, min), max);
}

// gets the angle represented by a quaternion (independent of rotation axis)
var quatangle = function (q) {
    return 2 * Math.acos(clamp(q[3], -1, 1));
}

var quatangle2 = function (q) {
    return 2 * Math.acos(clamp(q[0], -1, 1));
}

server.on('listening', function () {
    var address = server.address();
    console.log('UDP Server listening on ' + address.address + ":" + address.port);
});

//where the airplane first does yaw turn during taxiing onto the runway, then pitches during take-off, and finally rolls in the air.
// order: z-y-x in android coordinate system
var toEuler = function (qx, qy, qz, qw){
	// roll (x-axis rotation)// z axis here
	var sinr_cosp = +2.0 * (qw * qx + qy * qz);
	var cosr_cosp = +1.0 - 2.0 * (qx * qx + qy * qy);
	roll = Math.atan2(sinr_cosp, cosr_cosp);

	// pitch (y-axis rotation)// y here
	var sinp = +2.0 * (qw * qy - qz * qx);
	if (Math.abs(sinp) >= 1){
		pitch = sinp >= 0 ? Math.PI / 2 : -M_PI/2; // use 90 degrees if out of range
	}
	else
		pitch = Math.asin(sinp);

	// yaw (z-axis rotation) // x here
	var siny_cosp = +2.0 * (qw * qz + qx * qy);
	var cosy_cosp = +1.0 - 2.0 * (qy * qy + qz * qz);  
	yaw = Math.atan2(siny_cosp, cosy_cosp);
	
	var qDebug = Quaternion.fromEuler(yaw,roll, pitch,  'ZYX');
	//console.log("yaw:" + yaw  + "\troll" + roll+ "\tpitch:" + pitch);
	//console.log("qDebug:" + qDebug);	
	return [radian2degree(yaw), radian2degree(roll), radian2degree(pitch)];
}

var toEuler2 = function(qx,qy,qz,qw){
	var e = new THREE.Euler().setFromQuaternion(new THREE.Quaternion(qx, qy, qz, qw), "ZYX");
	return e;
}

var radian2degree = function(r){
	return r * 180 / Math.PI;
}

var predictByQuaternion = function (time, rotx, roty, rotz, rotw){
	rot_history.push([time, rotx, roty, rotz, rotw]);
	if (rot_history.length > history_length) {
        var prediction = [0, 0, 0, 1]
        for (var i = 1; i < 5; i++) {
            rot_history.splice(0, i);
            var x_vals = rot_history.map(a => [a[0] - rot_history[0][0], a[i]]);
            var coeffs = regression.polynomial(x_vals, { order: 4, precision: 10 }).equation;
            var next_time = x_vals[x_vals.length - 1][0] + lag;
            var next_val = poly(coeffs, next_time);
            prediction[i - 1] = next_val;
        }
        //console.log(next_val, coeffs);
        predictions.push([time + lag, prediction]);
    }
	
	var smallest_diff = 100;
    var error = 0;
    var p = [0, 0, 0, 1];
    for (var i = 0; i < predictions.length; i++) {
        var diff = Math.abs(predictions[i][0] - time);
        if (diff < smallest_diff) {
            smallest_diff = diff;
            var x = predictions[i][1][0];
            var y = predictions[i][1][1];
            var z = predictions[i][1][2];
            var w = predictions[i][1][3];
            var xdiff = Math.abs(x - rotx);
            var ydiff = Math.abs(y - roty);
            var zdiff = Math.abs(z - rotz);
            var wdiff = Math.abs(w - rotw);
            error = Math.sqrt(xdiff * xdiff + ydiff * ydiff + zdiff * zdiff + wdiff * wdiff);
            p = [x, y, z, w];
        }
    }
    var error_angle = quatangle(mulquat(p, conj([rotx, roty, rotz, rotw]))) * 180 / Math.PI;
    //console.log(error_angle.toFixed(5) + " predict: (" + p[0].toFixed(5), p[1].toFixed(5), p[2].toFixed(5), p[3].toFixed(5) + "), real: (" + rotx.toFixed(5), roty.toFixed(5), rotz.toFixed(5), rotw.toFixed(5) + ")");
}

var norm = function (v){
	//console.log(v);
	return Math.sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
}

var lerp = function (a, b, t) {
    return a * (1 - t) + b * t;
}

var roundRadians = function(r){
	if(r < -Math.PI)
		r += 2 * Math.PI;
	if(r > Math.PI)
		r -= 2 * Math.PI;
}

var predictByEuler = function(time, rotx, roty, rotz, rotw, gyrox, gyroy, gyroz){
    eulerAngles = toEuler2(rotx, roty, rotz, rotw);
    var alpha = clamp(norm([gyrox, gyroy, gyroz]), 0, 1);
	//console.log("euler:" + eulerAngles);
	//console.log("euler_history:" + euler_history);
	euler_history.push([time,eulerAngles.x, eulerAngles.y, eulerAngles.z]);
    if (euler_history.length > history_length) {
        var realEuler = euler_history[euler_history.length - 1].slice(1);
        var predictEuler = [0, 0, 0];
        euler_history.splice(0, 1);
        for (var i = 1; i < 4; i++) {
            var x_vals = euler_history.map(a => [a[0] - euler_history[0][0], a[i]]);
			//console.log(x_vals);
            var coeffs = regression.polynomial(x_vals, { order: 2, precision: 10 }).equation;
			//console.log("coeffs:" + coeffs);
            var next_time = x_vals[x_vals.length - 1][0] + lag;
			//console.log("next_time:" + next_time);
            var next_val = poly(coeffs, next_time);
            if (isNaN(coeffs[0]) || isNaN(coeffs[1]) || isNaN(coeffs[2])){
                next_val = realEuler[i - 1];
            }
			roundRadians(next_val[0]);
			roundRadians(next_val[1]);
			roundRadians(next_val[2]);
            //console.log("next_val:" + next_val);
            predictEuler[i - 1] = lerp(realEuler[i - 1], next_val, alpha);
        }
        //console.log(next_val, coeffs);
        predictsEuler.push([time + lag, predictEuler]);
    }
	
	var smallest_diff = 100;
    var error = Quaternion.ZERO;
	var err = [0, 0, 0];
    var p = [0, 0, 0, 0];
	var predictEulerAngles = [0,0,0];
    for (var i = 0; i < predictsEuler.length; i++) {
        var diff = Math.abs(predictsEuler[i][0] - time);
        if (diff < smallest_diff) {
            smallest_diff = diff;
            var x = predictsEuler[i][1][0];
            var y = predictsEuler[i][1][1];
            var z = predictsEuler[i][1][2];
			// turn to quaternion 
			var rx = x * Math.PI / 180;
			var ry = y * Math.PI / 180;
			var rz = z * Math.PI / 180;
			var qPredict = Quaternion.fromEuler(rx, ry, rx);
			var qReal = new Quaternion(rotw, rotx, roty, rotz);
			error = qReal.div(qPredict);
			
			//console.log("predict euler",x,y,z);
			//console.log("predict euler radian", rx,ry,rz);
			//console.log("qPredict quaternion:" + qPredict);
			//qPredict = Quaternion.fromEuler(x, y, z, 'ZYX');
			//console.log("qPredict:" + qPredict.toString());
			//console.log("qReal:" + qReal.toString());
            p = qPredict.toVector();//w,x,y,z
			predictEulerAngles[0] = x;
			predictEulerAngles[1] = y;
			predictEulerAngles[2] = z;
			// the above calcualtion is wrong so I only sum them up for now
			err = [eulerAngles.x - predictEulerAngles[0],
			eulerAngles.y - predictEulerAngles[1],
			eulerAngles.z - predictEulerAngles[2]];
        }
    }
	//console.log("error", error);
    var error_angle = quatangle2(error.toVector()) * 180 / Math.PI;
    console.log((norm(err) * 180 / Math.PI).toFixed(5) + "\t" + time + "\t" + predictEulerAngles[0].toFixed(5), predictEulerAngles[1].toFixed(5), predictEulerAngles[2].toFixed(5) + "\t" + eulerAngles.x.toFixed(5), eulerAngles.y.toFixed(5), eulerAngles.z.toFixed(5)); 
}

var predictByGyro = function(time, rotx, roty, rotz, rotw, gyrox, gyroy, gyroz, m){
    var alpha = clamp(norm([gyrox, gyroy, gyroz]), 0, 1);//?
	gyro_history.push([time, gyrox, gyroy, gyroz]);
    if (gyro_history.length > history_length) {
        var realGyro = gyro_history[gyro_history.length - 1].slice(1);
        var predictGyro = [0, 0, 0];
        gyro_history.splice(0, 1);
        for (var i = 1; i < 4; i++) {
            var x_vals = gyro_history.map(a => [a[0] - gyro_history[0][0], a[i]]);
			//console.log(x_vals);
            var coeffs = regression.polynomial(x_vals, { order: 2, precision: 10 }).equation;
			//console.log("coeffs:" + coeffs);
            var next_time = x_vals[x_vals.length - 1][0] + lag;
			//console.log("next_time:" + next_time);
            var next_val = poly(coeffs, next_time);
            if (isNaN(coeffs[0]) || isNaN(coeffs[1]) || isNaN(coeffs[2])){
                next_val = realGyro[i - 1];
            }
            //console.log("next_val:" + next_val);
            predictGyro[i - 1] = next_val;//lerp(realGyro[i - 1], next_val, alpha);
        }
        //console.log(next_val, coeffs);
        predGyroList.push([time + lag, predictGyro]);
    }
	
	var smallest_diff = 100;
    var error = Quaternion.ZERO;
	var err = [0, 0, 0];
    var p = [0, 0, 0, 0];
	var curPredictGyro = [0,0,0];
    for (var i = 0; i < predGyroList.length; i++) {
        var diff = Math.abs(predGyroList[i][0] - time);
        if (diff < smallest_diff) {
            smallest_diff = diff;
            var x = predGyroList[i][1][0];
            var y = predGyroList[i][1][1];
            var z = predGyroList[i][1][2];
			curPredictGyro = predGyroList[i][1];
			
			err = [curPredictGyro[0] - gyrox,
			curPredictGyro[1] - gyroy,
			curPredictGyro[2] - gyroz];
        }
    }
    console.log((norm(err)).toFixed(5) + "\t" + time + "\t" + curPredictGyro[0].toFixed(5), curPredictGyro[1].toFixed(5), curPredictGyro[2].toFixed(5) + "\t" + gyrox.toFixed(5), gyroy.toFixed(5), gyroz.toFixed(5) + "\t"
	+ m[0].toFixed(5) + "\t"
	+ m[1].toFixed(5) + "\t"
	+ m[2].toFixed(5) + "\t"
	+ m[3].toFixed(5) + "\t"
	+ m[4].toFixed(5) + "\t"
	+ m[5].toFixed(5) + "\t"
	+ m[6].toFixed(5) + "\t"
	+ m[7].toFixed(5) + "\t"
	+ m[8].toFixed(5)); 
}

server.on('message', function (message, remote) {
    //console.log(remote.address + ':' + remote.port);
	var gyrox = message.readFloatBE(0);
	var gyroy = message.readFloatBE(4);
	var gyroz = message.readFloatBE(8);
    //console.log("gyro:(" + gyrox + "," + gyroy + "," + gyroz + ")");
    var rotx = message.readFloatBE(12);
    var roty = message.readFloatBE(16);
    var rotz = message.readFloatBE(20);
    var rotw = message.readFloatBE(24);
    //console.log("rot:(" + rotx + "," + roty + "," + rotz + "," + rotw + ")");
	// preprocess: convert to euler angle

    var time = message.readFloatBE(28);
	var m = new Float32Array(9);
	for(let i = 0; i < 9; i++)
		m[i] = message.readFloatBE(32 + 4*i);
    if (time != last_time) {
        //console.log(time);
        // test with quaternion	
        //predictByQuaternion(time, rotx, roty, rotz, rotw);
        // test with eulerAngles
        predictByGyro(time, rotx, roty, rotz, rotw, gyrox, gyroy, gyroz, m);
        last_time = time;
    }
    
});

server.bind(PORT, HOST);