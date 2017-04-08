function pnl = linear_mean_revert(file, lookback)
    file = fopen(file);
    output = textscan(file, '%s%f', 'delimiter', ',');
    y = output(2);
    y = y{1,1};
    ylag = lag(y,1);
    ma = tsmovavg(y, 's', lookback, 1);
    % ma(1) = 0;
    mktval = - (y - ma)./movstd(y,lookback);
    pnl = lag(mktval, 1) .* (y-ylag) ./ ylag;