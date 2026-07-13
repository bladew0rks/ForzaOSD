import g
from conf  import *
from dash  import *
import ac
from utils import *

def init_all():    

  # load the number of windows
  windows = 0
  windows = int(loadCFG(g.appPATH+'cfg/','master_config.ini','GENERAL','windows',windows))  

  for w in range(windows):
    g.dashes.append(Dash(g.appPATH,w+1))  
  