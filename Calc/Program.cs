// See https://aka.ms/new-console-template for more information

using CalcLib;

var calc = new Calculator("1+2*((2*3)+5)");

var result = calc.Compute();
Console.WriteLine(result);
