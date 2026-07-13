# defining globals

import os
import sys
import os.path
import platform

appPATH = os.path.dirname(__file__) + '/'

if platform.architecture()[0] == "64bit":
  sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'pdash_dll_x64'))
else:
  sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'pdash_dll_x86'))

os.environ['PATH'] = os.environ['PATH'] + ";."  
  
from pdash_third_party.sim_info import SimInfo

dashes   = []
sim_info = SimInfo()
debug    = False
maxrpm   = 0
maxrpm_calculated = False