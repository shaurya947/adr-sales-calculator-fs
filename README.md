# adr-sales-calculator-fs
Program to calculate sales of "A Dark Room" (and other products) using F#

This project was developed, compiled and executed using Xamarin Studio on MacOS X.

The program scans the files located [here](https://github.com/amirrajan/amirrajan.github.com/tree/master/adr-sales) and prints a report of total earnings across all products by year, by month.

It uses:
- [Http.fs] (https://github.com/relentless/Http.fs) to pull down JSON / file data
- [fixer.io](http://fixer.io/) to get historic currency rates
