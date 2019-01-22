require('log-timestamp');
var regression = require('regression');

var PORT = 12345;
var HOST = '216.165.71.223';

var dgram = require('dgram');
var server = dgram.createSocket('udp4');

var rot_history = [];
var history_length = 100;
// how far ahead we are trying to predict, in seconds
var lag = 0.030;
var predictions = [];

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

// gets the angle represented by a quaternion (independent of rotation axis)
var quatangle = function (q) {
    return 2 * Math.acos(Math.abs(Math.min(Math.max(q[3], -1), 1)));
}

server.on('listening', function () {
    var address = server.address();
    console.log('UDP Server listening on ' + address.address + ":" + address.port);
});

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

    var time = message.readFloatBE(28);
    //console.log(time);
    rot_history.push([time, rotx, roty, rotz, rotw]);
    if (rot_history.length > history_length) {
        var prediction = [0, 0, 0, 1]
        for (var i = 1; i < 5; i++) {
            rot_history.splice(0, i);
            var x_vals = rot_history.map(a => [a[0] - rot_history[0][0], a[i]]);
            var coeffs = regression.polynomial(x_vals, { order: 3, precision: 10 }).equation;
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
    console.log(error_angle.toFixed(5) + " predict: (" + p[0].toFixed(5), p[1].toFixed(5), p[2].toFixed(5), p[3].toFixed(5) + "), real: (" + rotx.toFixed(5), roty.toFixed(5), rotz.toFixed(5), rotw.toFixed(5) + ")");
});

server.bind(PORT, HOST);