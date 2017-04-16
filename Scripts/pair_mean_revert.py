import numpy as np
import matplotlib.pyplot as plt

f = open("../data/polo_LTC_BTC1hour.csv")
lines = f.readlines()
f.close()

for l in lines:
	print (float(l.split(',')[1]))

p1 = [float(l.split(',')[1]) for l in lines]

f = open("../data/polo_XRP_BTC1hour.csv")
lines = f.readlines()
f.close()
p2 = [float(l.split(',')[1]) for l in lines]

