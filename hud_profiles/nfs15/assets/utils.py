import ac
import g
from conf import *

def tocons(s):
  ac.console(repr(s))

def tolog(s):
  ac.log(repr(s))

def noborder(element):
  ac.drawBorder(element,0)

def setpos(element,posx,posy):
  ac.setPosition(element,posx,posy)

def setsize(element,sizex,sizey):
  ac.setSize(element,sizex,sizey)

def texture(element,path):
  ac.setBackgroundTexture(element, path)

def transparent(element):
  ac.setBackgroundOpacity(element, 0)

def offscreen(element):
  setpos(element, 0 , -10000)

def inscreen(element):
  setpos(element, 0 , 0)

def noicon(element):
  ac.setIconPosition(element, 0, -10000)
  
def readbool(path,filename,section,variable):  
  
  dummy = False
  return True if loadCFG(path,filename,section,variable,dummy).upper() == 'TRUE' else False
