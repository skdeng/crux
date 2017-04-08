import numpy as np
import matplotlib.pyplot as plt
import math

file = open("../data/bfx_hourly_nov.csv")
lines = file.readlines()
file.close()

price = [float(l.split(',')[1]) for l in lines]

pl = []
apl = []

MA_LAG = 24
ma = [0 if i < MA_LAG-1 else np.average(price[i-MA_LAG+1:i+1]) for i in range(len(price))]
mstd = [0 if i < MA_LAG-1 else np.std(price[i-MA_LAG+1:i+1]) for i in range(len(price))]

usd = 1000
btc = 10

start_value = usd + btc * price[MA_LAG-1]

zval = []

walusd = []
walbtc = []

np.seterr(all='raise')

for i in range(MA_LAG-1, len(price)):
	z = -(price[i] - ma[i]) / mstd[i]

	val = usd + btc * price[i]

	# if z > 0: #buy
	# 	new_usd = val / (2+z)
	# 	diff = usd - new_usd
	# 	usd = new_usd
	# 	btc += diff / price[i]
	# else:	#sell
	# 	new_btc = val / (2-z)
	# 	diff = btc * price[i] - new_btc
	# 	btc = new_btc / price[i]
	# 	usd += diff

	# if np.abs(z) < 1:
	# 	continue

	vol = 1 if z > 0 else 0
	# vol = -z * 10 / price[i]
	if z < 0:
		vol = min(vol, usd/price[i])
	else:
		vol = max(vol, -btc)
	usd -= vol * price[i]
	btc += vol

	zval.append(z)
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

zval = [0] * (MA_LAG-1) + zval

total_val = [walusd[i] + walbtc[i] * price[MA_LAG-1+i] / 100 for i in range(len(walusd))]

plt.plot(pl, label='Strategy P/L MA:{}'.format(MA_LAG))
plt.plot(apl, label='Asset P/L')
# plt.plot(walusd)
# plt.plot(walbtc)

plt.title("Ethereum Hourly Mean Reversal")
plt.legend(loc='upper left')
plt.show()