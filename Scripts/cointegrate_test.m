function ret = cointegrate_test(file1, file2)
    y1 = csvread(file1);
    y1 = y1(:,2);
    y2 = csvread(file2);
    y2 = y2(:,2);
    
    res = cadf(y1,y2,0,1);
    prt(res);
    
    y = [y1,y2];
    res = johansen(y, 0,1);
    prt (res);
    
    yport = sum(repmat(res.evec(:,1)', [size(y,1) 1]) .* y, 2);
    ylag = lag(yport, 1);
    deltaY = yport - ylag;
    deltaY(1) = [];
    ylag(1) = [];
    reg = ols(deltaY, [ylag ones(size(ylag))]);
    hl = -log(2) / reg.beta(1);
    ret = hl;