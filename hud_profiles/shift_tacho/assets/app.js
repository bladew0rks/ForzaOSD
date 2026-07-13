// digital tacho UI app by GhostInTheLeague

angular.module('beamng.apps')

.directive('ghostDigitalTacho', function () {
  return {
  templateUrl: '/ui/modules/apps/GhostsDigitalTacho/app.html',
  replace: true,
  restrict: 'EA',
  link: function Main($scope, element, attrs, ctrl) {      

    // loading settings

    var dynamic_movement = (localStorage.getItem('GDT_dynamic_movement') === 'true');
    var app_layout = Number(localStorage.getItem('GDT_layout'));

    if (app_layout == undefined) {
      app_layout = 1;
    }

    // streams

    var streamsList = ['electrics', 'sensors', 'engineInfo', 'stats', 'escData', 'tcsData', 'escInfo'];
    StreamsManager.add(streamsList);

    // setting up variables

    var redline_threshold = 500; //max RPM - threshold > where the redlining begins 
    const button_active_class = 'GDT_img-button-active';
    const redline_active_class = 'GDT_inset-display-redlining';
    const rev_active_class = 'GDT_rev-light-active';
    const blinker_active_class = 'GDT_blinker-active'; 

    const hidden_class = 'GDT_hidden';

    const app_base = document.getElementById("GDT_reactive");
    const scale_wrapper = document.getElementById("GDT_scale-wrapper");

    $scope.GDT_gear = 'N';
    $scope.GDT_speed = '0';
    $scope.GDT_unit = 'GDT_metric';

    $scope.GDT_right_blink_active = '';
    $scope.GDT_left_blink_active = '';

    $scope.GDT_shiftlights_hidden = hidden_class;
    $scope.GDT_blink_hidden = hidden_class;

    const root_folder = "/ui/modules/apps/GhostsDigitalTacho/";

    $scope.GDT_ebrake_active = '';
    $scope.GDT_abs_active = '';
    $scope.GDT_traction_active = '';
    $scope.GDT_redline_active = '';

    $scope.GDT_rev0_active = '';
    $scope.GDT_rev1_active = '';
    $scope.GDT_rev2_active = '';
    $scope.GDT_rev3_active = '';
    $scope.GDT_rev4_active = '';
    $scope.GDT_rev5_active = '';
    $scope.GDT_rev6_active = '';

    $scope.shattered = '';

    // functions accessed from HTML

    $scope.GDT_toggleMove = function toggleDynamicMovement() {
      dynamic_movement = !dynamic_movement;
      localStorage.setItem('GDT_dynamic_movement', dynamic_movement);
      resetDynamicMovement(app_base);
    }

    $scope.GDT_cycleLayout = function cycleLayout(step) {
      console.log(app_layout)
      app_layout += step;
      if(app_layout > 2) {app_layout = 0;}

      switch(app_layout){
        case 0: 
          $scope.GDT_blink_hidden = hidden_class;
          $scope.GDT_shiftlights_hidden = hidden_class;
          break;
        case 1: 
          $scope.GDT_blink_hidden = hidden_class;
          $scope.GDT_shiftlights_hidden = '';
          break;
        case 2: 
          $scope.GDT_blink_hidden = '';
          $scope.GDT_shiftlights_hidden = '';
          break;
      }
    }

    $scope.GDT_setUnits = function setUnits(newUnit) {
      $scope.GDT_unit = ( newUnit=='metric' ? 'GDT_metric' : 'GDT_imperial');
    }

    // functions

    function mpsToUnit(speed, isMetric) {
      // converts meters/second to either metric or imperial
      if (isMetric) {
        return speed * 3.6;
      } else {
        return speed * 2.2369;
      }
    }

    function clamp(value, minimum, maximum) {
      //simple clamp
      return Math.min(maximum, Math.max(minimum, value))
    }

    function doDynamicMovement(appBaseDOM, sensors) {
      let win_width = document.documentElement.clientWidth;
      let win_height = document.documentElement.clientHeight
      let rect = appBaseDOM.getBoundingClientRect();

      let center_x = Math.round(rect.x + rect.width*0.5);
      let center_y = Math.round(rect.y + rect.height*0.5);
      let offset_x = win_width*.5 - center_x;
      let offset_y = win_height*.5 - center_y

      let moveMultiplier = Math.min(win_width/1920, win_height/1080);

      let finalMoveX = clamp(sensors.gx * moveMultiplier * 4, -75, 75);
      let finalMoveY = clamp((-sensors.gz + sensors.gravity)*10 * moveMultiplier, -100, 100)
      let finalMoveZ = clamp(1 + sensors.gy * moveMultiplier * 0.002, 0.95, 1.15)

      appBaseDOM.style.transformOrigin = offset_x + "px " + offset_y+"px ";
      appBaseDOM.style.transform = "translate(" + 
                                  finalMoveX + "px, " + 
                                  finalMoveY + "px) scale(" +  //clamp(pitchChange*1500, -100, 100) + "px) scale(" + 
                                  finalMoveZ + ")"; //1.07
    }

    function resetDynamicMovement(appBaseDOM) {
      appBaseDOM.style.transform = "translate(0px, 0px) scale(1)";
    }

    // Setting up
    
    resetDynamicMovement(app_base);
    $scope.GDT_cycleLayout(0);

    // Special thanks to Zeit for showing me this api! 

    bngApi.engineLua("settings.getValue('uiUnits')", function (unit) {
      $scope.GDT_setUnits(unit);
    });


    

    // *** Destroy event *** //
    $scope.$on('$destroy', function () {
      localStorage.setItem('GDT_dynamic_movement', dynamic_movement);
      localStorage.setItem('GDT_layout', app_layout);

      StreamsManager.remove(streamsList);
      $scope.stop;

    });

    // *** Main event *** //

    $scope.$on('streamsUpdate', function (event, streams) {

      // turn signals

      $scope.GDT_left_blink_active = (streams.electrics.signal_L==0) ? '' : blinker_active_class
      $scope.GDT_right_blink_active = (streams.electrics.signal_R==0) ? '' : blinker_active_class

      // Gearbox redlining

      redline_threshold = (streams.engineInfo[1]*.05)*3

      if (streams.engineInfo[4] >= streams.engineInfo[1]-redline_threshold) {
        $scope.GDT_redline_active = redline_active_class;
      } else {
        $scope.GDT_redline_active = '';
      }

      // Rev lights

      for (const x of Array(7).keys()) {
        var setClass = ''
        if (streams.engineInfo[4] >= streams.engineInfo[1] - (x+1)*(streams.engineInfo[1]*.05) ) {
          setClass = rev_active_class;
        }
        switch(x) {
          case 6: 
            $scope.GDT_rev0_active = setClass;
            break;
          case 5: 
            $scope.GDT_rev1_active = setClass;
            break;
          case 4: 
            $scope.GDT_rev2_active = setClass;
            break;
          case 3: 
            $scope.GDT_rev3_active = setClass;
            break;
          case 2: 
            $scope.GDT_rev4_active = setClass;
            break;
          case 1: 
            $scope.GDT_rev5_active = setClass;
            break;
          case 0: 
            $scope.GDT_rev6_active = setClass;
            break;
        }
        // ^ this solution is pretty bad but I couldn't figure out how to modify a $scope variable through an array. Will get back to it later.
        // TODO: make a more elegant solution lmao
      }

      // Parking brake
      if (streams.electrics.parkingbrake > 0) {
        $scope.GDT_ebrake_active = button_active_class;
      } else {
        $scope.GDT_ebrake_active = '';
      }

      // ABS
      if (streams.electrics.abs > 0) {
        $scope.GDT_abs_active = button_active_class;
      } else {
        $scope.GDT_abs_active = '';
      }

      // Traction Control / ESC
      try {
        if (streams.electrics.esc
        && streams.escInfo.ledColor != 999999 // "off" color
        && streams.electrics.tcs != 0) {
          $scope.GDT_traction_active = button_active_class;
        } else {
          $scope.GDT_traction_active = '';
        }
      } catch (e) {
        $scope.GDT_traction_active = '';
      }

      // if(streams.electrics.esc) {
      //   $scope.GDT_traction_active = button_active_class;

      // } else {
      //   $scope.GDT_traction_active = '';
      // }
      
      // Setting gear and speed
    
          // ----------------------- This section is from Tacho2, ever so slightly modified.
      if(streams.engineInfo[13] == "manual") {
        var gear = streams.engineInfo[5];
        var gearStr = gear.toString();

        if(gear == 0) gearStr = 'N';
        else if(gear == -1) gearStr = 'R';
        else if(gear < -1) gearStr = 'R' + (-gear);

        $scope.GDT_gear = gearStr
      } else {
        $scope.GDT_gear = ["P","R","N","D","2","1"][Math.round(streams.electrics.gear_A*5)];
      }
          // -----------------------

      $scope.GDT_speed = Math.round( mpsToUnit(streams.electrics.wheelspeed, ($scope.GDT_unit == 'GDT_metric')) ); //meters/second to km/h
      
      // calculating dynamic movement
      if( dynamic_movement === true ) {
        doDynamicMovement(app_base, streams.sensors);
      }

      // Setting app scale to match width/height set by user

      app_scale = Math.min(
                    Math.max(1,app_base.clientWidth)/440, 
                    Math.max(1,app_base.clientHeight)/280);
      scale_wrapper.style.transform = "scale(" + app_scale + ")";

      if( (streams.stats.beams_deformed / Math.max(1,streams.stats.beam_count)) >= 0.07) {
        $scope.GDT_shattered = 'GDT_shatter';
      } else {
        $scope.GDT_shattered = '';
      }

      // updating

      $scope.$apply(); 
    });

  }
};
});