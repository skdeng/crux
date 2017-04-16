function momentum_test(filename)
    file = fopen(filename);
    out = textscan(file, '%s%f', 'delimiter', ',');
    fclose(file);
    
    y = out{2};
    
    for lookback = [1 5 10 25 60 120 250]
        for hold = [1 5 10 25 60 120 250]
            ret_lag = (y - backshift(lookback, y)) ./ backshift(lookback, y);
            ret_fut = (fwdshift(holddays, y) - y) ./ y;
            badDates = any([isnan(ret_lag) isnan(ret_fut)], 2);
            
            ret_lag(badDates) = [];
            ret_fut(badDates) = [];
            
            if (lookback >= holddays)
                indepSet = [1 : lookback:length(ret_lag)];
            else
                indepSet = [1 : holddays:length(ret_lag)];
            end
            
            ret_lag = ret_lag(indepSet);
            ret_fut = ret_fut(indepSet);
            
            [cc, pval] = corrcoef(ret_lag, ret_fut);
            fprintf(1, 'lookback=%3i holddays=%3i cc=%7.4f pval=%6.4f\n', lookback, holddays, cc(1,2), pval(1,2));
        end
    end
end
            