function ret = port_mean_revert(file1, file2, lookback)
    y1 = csvread(file1);
    y1 = y1(:,2);
    y2 = csvread(file2);
    y2 = y2(:,2);
    
    y = [y1,y2];
    j = johansen(y,0,1);
    %ratio = j.evec(:,1)';
    ratio = [1.0 -347.786];
    yport = sum(repmat(ratio, [size(y,1) 1]) .* y, 2);
    
    ma = tsmovavg(yport, 's', lookback, 1);
    ms = movstd(yport, lookback);
    numUnits = -(yport-ma) ./ ms;
    pos = repmat(numUnits, [1 size(y,2)]) .* repmat(ratio, [size(y,1) 1]) .* y;
    pnl = sum(lag(pos, 1) .* (y - lag(y, 1)) ./ lag(y,1), 2);
    ret = pnl ./ sum(abs(lag(pos, 1)), 2);
    