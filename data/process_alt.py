import time
import datetime
import sys

def isfloat(s):
	try:
		float(s)
		return True
	except ValueError:
		return False

file = open(sys.argv[1])
lines = file.readlines()
file.close()

for i in range(len(lines)):
	tok = lines[i].split(', ')
	t = tok[0] + tok[1]
	lines[i] = t + ',' + tok[3]

for i in range(len(lines)):
	tok = lines[i].split(',')
	if not isfloat(tok[1]):
		print (i)
		lines[i] = tok[0] + ',' + str((float(lines[i-1].split(',')[1]) + float(lines[i+1].split(',')[1])) / 2)
		lines[i] += '\n'

file = open(sys.argv[1], 'w')
file.writelines(lines)
file.close