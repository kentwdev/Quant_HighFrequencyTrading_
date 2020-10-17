library(timeSeries)
library(rugarch)

final.aic <- Inf
final.order <- c(0,0,0)

# estimate optimal ARIMA model order
for (p in 0:5) for (q in 0:5) { # limit possible order to p,q <= 5
	if (p == 0 && q == 0) next # p and q can't both be zero
    arimaFit <- tryCatch(arima(roll.returns, order = c(p,0,q)), 
                         error = function( err ) FALSE,
                         warning = function( err ) FALSE )
    if (!is.logical( arimaFit)) {
		current.aic <- AIC(arimaFit)
		if (current.aic < final.aic) { # retain order if AIC is reduced
			final.aic <- current.aic
			final.order <- c(p,0,q)
			final.arima <- arima(roll.returns, order = final.order)
		}
    }
    else next 
}
 
# specify and fit the GARCH model
spec = ugarchspec(variance.model <- list(garchOrder=c(1,1)),
                  mean.model <- list(
						armaOrder <- c(final.order[1], final.order[3]), include.mean = T),
						distribution.model = "sged")
fit = tryCatch(ugarchfit(spec, roll.returns, solver = 'hybrid'), error = function(e) e, warning = function(w) w)
  
# calculate next day prediction from fitted mode
# model does not always converge - assign value of 0 to prediction and p.val in this case
if (is(fit, "warning")) {
    forecasts <- 0 
    p.val <- 0
}
else {
    next.day.fore = ugarchforecast(fit, n.ahead = 1)
    x = next.day.fore@forecast$seriesFor
    directions <- ifelse(x[1] > 0, 1, -1) # directional prediction only
    forecasts <- x[1] # actual value of forecast

    # analysis of residuals
    resid <- as.numeric(residuals(fit, standardize = TRUE))
    ljung.box <- Box.test(resid, lag = 20, type = "Ljung-Box", fitdf = 0)
    p.val <- ljung.box$p.value
  }
}