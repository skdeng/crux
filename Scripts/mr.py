import numpy as np
import matplotlib.pyplot as plt
import math
from scipy import stats

file = open("../data/polo_LTC_USD1hour.csv")
lines = file.readlines()
file.close()

price = [float(l.split(',')[1]) for l in lines]

pl = []
apl = []
trade = []

FRICTION = 0.005

usd = 100
coin = 10

LR_LENGTH = 168

start_value = usd + coin * price[LR_LENGTH-1]

np.seterr(all='raise')

MA = []

for i in range(LR_LENGTH-1, len(price)):
	a,b,r,_,_ = stats.linregress(np.arange(LR_LENGTH), price[i-LR_LENGTH+1:i+1])

	mse = 0
	for j in range(LR_LENGTH, 0, -1):
		mse += ((a*j+b) - price[i-j+1]) ** 2

	mse = mse / LR_LENGTH
	ma_length = int(min(12 * math.exp(mse), LR_LENGTH))
	ma = np.average(price[i-ma_length+1:i+1])
	mstd = np.std(price[i-ma_length+1:i+1])
	mstd = max(mstd, 0.001)
	MA.append(ma)
	z = -(price[i] - ma) / mstd

	val = usd + coin * price[i]

	if z > 0: #buy
		new_usd = val / (2+z)
		diff = usd - new_usd
		trade.append(0 if diff > 0 else 1)
		usd = new_usd
		coin += (diff / price[i]) * (1 - FRICTION)
	else:	#sell
		new_coin = val / (2-z)
		diff = coin * price[i] - new_coin
		trade.append(0 if diff < 0 else 1)
		coin = new_coin / price[i]
		usd += diff * (1 - FRICTION)

	pl.append((usd + coin * price[i]) / start_value)
	apl.append(price[i]/price[LR_LENGTH-1])

val = usd + coin * price[-1]
print ("Total: {}".format(val))
print ("USD: {}".format(usd))
print ("BTC: {}".format(coin))
print ("Final PL: {}".format(val / start_value))
print ("Asset PL: {}".format(price[-1] / price[LR_LENGTH-1]))

# plt.plot(pl, label='Strategy P/L')
# plt.plot(apl, label='Asset P/L')

trade = np.array(trade)
price = np.array(price[LR_LENGTH-1:])
buys = np.where(trade == 0)[0]
sells = np.where(trade == 1)[0]

plt.plot(price)
plt.plot(MA)
plt.scatter(buys, price[buys], c='green')
plt.scatter(sells, price[sells], c='red')

plt.show()