/*
local site scripts, common to all pages
*/

function serverTimeLoader() {

    //$("#datetime").load("/DateTime");

    $.ajax({
        url: "/DateTime",
        timeout: 1000,
        type: "get",
        success: function (result) {
            $("#datetime").text(result);
        },
        error: function (request, status, err) {
            if (status == "timeout") {
                $("#datetime").text("server not reached - timeout");
            } else {
                $("#datetime").text("server not reached: " + err + " status: " + status);
            }
        }
    });

    var refresh = 1000; // milliseconds
    mytime = setTimeout('serverTimeLoader();', refresh)
}

function errorsDataLoader() {
    $.ajax({
        url: "/ErrorsData",
        timeout: 1000,
        type: "get",
        success: function (result) {
            $("#errorsPanel").html(result);
        },
        error: function (request, status, err) {
            if (status == "timeout") {
                $("#errorsPanel").text("server not reached - timeout");
            } else {
                $("#errorsPanel").text("server not reached: " + err + " status: " + status);
            }
        }
    });

    var refresh = 2000; // milliseconds
    mytime = setTimeout('errorsDataLoader();', refresh)
}

function joystickDataLoader() {
    $.ajax({
        url: "/JoystickData",
        timeout: 1000,
        type: "get",
        success: function (result) {
            $("#joystickDataPanel").html(result);
        },
        error: function (request, status, err) {
            if (status == "timeout") {
                $("#joystickDataPanel").html("<p>server not reached - timeout</p>");
            } else {
                $("#joystickDataPanel").html("<p>server not reached: " + err + " status: " + status + "</p>");
            }
        }
    });

    var refresh = 1000; // milliseconds
    mytime = setTimeout('joystickDataLoader();', refresh)
}

function robotStateLoader() {
    $.ajax({
        url: "/RobotState",
        timeout: 1000,
        type: "get",
        success: function (result) {
            $("#robotStatePanel").html(result);
        },
        error: function (request, status, err) {
            if (status == "timeout") {
                $("#robotStatePanel").html("<p>server not reached - timeout</p>");
            } else {
                $("#robotStatePanel").html("<p>server not reached: " + err + " status: " + status + "</p>");
            }
        }
    });

    var refresh = 1000; // milliseconds
    mytime = setTimeout('robotStateLoader();', refresh)
}

function sensorsDataLoader() {
    $.ajax({
        url: "/SensorsData",
        timeout: 1000,
        type: "get",
        success: function (result) {
            $("#sensorsDataPanel").html(result);
        },
        error: function (request, status, err) {
            if (status == "timeout") {
                $("#sensorsDataPanel").html("<p>server not reached - timeout</p>");
            } else {
                $("#sensorsDataPanel").html("<p>server not reached: " + err + " status: " + status + "</p>");
            }
        }
    });

    var refresh = 1000; // milliseconds
    mytime = setTimeout('sensorsDataLoader();', refresh)
}
