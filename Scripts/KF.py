import matplotlib.pyplot as plt
import numpy as np

file = open("../data/polo_LTC_USD1hour.csv")
lines = file.readlines()
file.close()

price = [float(l.split(',')[1]) for l in lines]

pl = []
apl = []
trade = []

FRICTION = 0.005
MA_LAG = 72
mstd = [0 if i < MA_LAG-1 else np.std(price[i-MA_LAG+1:i+1]) for i in range(len(price))]
kval = []

usd = 100
btc = 1

start_value = usd + btc * price[MA_LAG-1]

walusd = []
walbtc = []

class kalman():
	def __init__(self):
		self.p = 1.0
		self.q = 0.000001
		self.r = 0.01
		self.x = 0.0

		self.k = 0

	def m_update(self):
		self.k = (self.p+self.q)/(self.p+self.q+self.r)
		# print (k)
		self.p = self.r*(self.p+self.q)/(self.r+self.p+self.q)

	def update(self, i):
		self.m_update()
		self.x = self.x + (i-self.x)*self.k
		return self.x
k = kalman()

np.seterr(all='raise')

for i in range(MA_LAG-1, len(price)):
	kval.append(k.update(price[i]))
	z = -(price[i] - kval[-1]) / mstd[i]

	val = usd + btc * price[i]

	if z > 0: #buy
		new_usd = val / (2+z)
		diff = usd - new_usd
		trade.append(0 if diff > 0 else 1)
		usd = new_usd
		btc += (diff / price[i]) * (1 - FRICTION)
	else:	#sell
		new_btc = val / (2-z)
		diff = btc * price[i] - new_btc
		trade.append(0 if diff < 0 else 1)
		btc = new_btc / price[i]
		usd += diff * (1 -  FRICTION)

	pl.append((usd + btc * price[i]) / start_value)
	apl.append(price[i]/price[MA_LAG-1])
	walusd.append(usd)
	walbtc.append(btc)

val = usd + btc * price[-1]
print ("Total: {}".format(val))
print ("USD: {}".format(usd))
print ("BTC: {}".format(btc))
print ("Final PL: {}".format(val / start_value))
print ("Asset PL: {}".format(price[-1] / price[MA_LAG-1]))


total_val = [walusd[i] + walbtc[i] * price[MA_LAG-1+i] / 100 for i in range(len(walusd))]

# plt.plot(pl, label='Strategy P/L MA:{}'.format(MA_LAG))
# plt.plot(apl, label='Asset P/L')
# plt.plot(walusd)
# plt.plot(walbtc)

trade = np.array(trade)
price = np.array(price[MA_LAG-1:])
buys = np.where(trade == 0)[0]
sells = np.where(trade == 1)[0]

plt.plot(price)
plt.plot(kval)
plt.scatter(buys, price[buys], c='green')
plt.scatter(sells, price[sells], c='red')

plt.legend(loc='upper left')
plt.show()