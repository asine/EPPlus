﻿/*******************************************************************************
* You may amend and distribute as you like, but don't remove this header!
*
* EPPlus provides server-side generation of Excel 2007/2010 spreadsheets.
* See http://www.codeplex.com/EPPlus for details.
*
* Copyright (C) 2011-2017 Jan Källman, Matt Delaney, and others as noted in the source history.
*
* This library is free software; you can redistribute it and/or
* modify it under the terms of the GNU Lesser General Public
* License as published by the Free Software Foundation; either
* version 2.1 of the License, or (at your option) any later version.

* This library is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
* See the GNU Lesser General Public License for more details.
*
* The GNU Lesser General Public License can be viewed at http://www.opensource.org/licenses/lgpl-license.php
* If you unfamiliar with this license or have questions about it, here is an http://www.gnu.org/licenses/gpl-faq.html
*
* All code and executables are provided "as is" with no warranty either express or implied. 
* The author accepts no liability for any damage or loss of business that this product may cause.
*
* For code change notes, see the source control history.
*******************************************************************************/
using System;
using System.Linq;
using EPPlusTest.FormulaParsing.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing;
using OfficeOpenXml.FormulaParsing.Excel.Functions;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace EPPlusTest.FormulaParsing.Excel.Functions.Math
{
	[TestClass]
	public class AverageTests : MathFunctionsTestBase
	{
		#region Average Function Tests
		[TestMethod]
		public void AverageWithFourNumbersReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs(1.5, 2, 3.5, 7);
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual((1.5 + 2 + 3.5 + 7) / 4, result.Result);
		}

		[TestMethod]
		public void AverageWithFourNegativeNumbersReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs(-1.5, -2, -3.5, -7);
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual((-1.5 + -2 + -3.5 + -7) / 4, result.Result);
		}

		[TestMethod]
		public void AverageWithOneIntegerReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs(2);
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(2d, result.Result);
		}

		[TestMethod]
		public void AverageWithOneDoubleReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs(2.5);
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(2.5, result.Result);
		}

		[TestMethod]
		public void AverageWithOneNumericStringReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs("2");
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(2d, result.Result);
		}

		[TestMethod]
		public void AverageWithOneNonNumericStringReturnsPoundValue()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs("word");
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void AverageWithDateInStringReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs("6/16/2017");
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(42902d, result.Result);
		}

		[TestMethod]
		public void AverageWithOneBooleanValueReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs(true);
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(1d, result.Result);
		}

		[TestMethod]
		public void AverageInWorksheetWithSingleInputsWorksAsExpected()
		{
			using (var package = new ExcelPackage())
			{
				var worksheet = package.Workbook.Worksheets.Add("Sheet1");
				worksheet.Cells["B2"].Formula = "AVERAGE(2)";
				worksheet.Cells["B3"].Formula = "AVERAGE(2.5)";
				worksheet.Cells["B4"].Formula = "AVERAGE(\"2\")";
				worksheet.Cells["B5"].Formula = "AVERAGE(\"word\")";
				worksheet.Cells["B6"].Formula = "AVERAGE(\"6/16/2017\")";
				worksheet.Cells["B7"].Formula = "AVERAGE(TRUE)";
				worksheet.Cells["B8"].Formula = "AVERAGE(1.5, 2, 3.5, 7)";
				worksheet.Cells["B9"].Formula = "AVERAGE(-1.5, -2, -3.5, -7)";
				worksheet.Calculate();
				Assert.AreEqual(2d, worksheet.Cells["B2"].Value);
				Assert.AreEqual(2.5, worksheet.Cells["B3"].Value);
				Assert.AreEqual(2d, worksheet.Cells["B4"].Value);
				Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)worksheet.Cells["B5"].Value).Type);
				Assert.AreEqual(42902d, worksheet.Cells["B6"].Value);
				Assert.AreEqual(1d, worksheet.Cells["B7"].Value);
				Assert.AreEqual(3.5, worksheet.Cells["B8"].Value);
				Assert.AreEqual(-3.5, worksheet.Cells["B9"].Value);
			}
		}

		[TestMethod]
		public void AverageFunctionWithErrorValuesAsInputReturnsTheInputErrorValue()
		{
			var func = new Average();
			var argNA = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.NA), 1, 1, 1, 1);
			var argNAME = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.Name), 1, 1, 1, 1);
			var argVALUE = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.Value), 1, 1, 1, 1);
			var argNUM = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.Num), 1, 1, 1, 1);
			var argDIV0 = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.Div0), 1, 1, 1, 1);
			var argREF = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.Ref), 1, 1, 1, 1);
			var resultNA = func.Execute(argNA, this.ParsingContext);
			var resultNAME = func.Execute(argNAME, this.ParsingContext);
			var resultVALUE = func.Execute(argVALUE, this.ParsingContext);
			var resultNUM = func.Execute(argNUM, this.ParsingContext);
			var resultDIV0 = func.Execute(argDIV0, this.ParsingContext);
			var resultREF = func.Execute(argREF, this.ParsingContext);
			Assert.AreEqual(eErrorType.NA, ((ExcelErrorValue)resultNA.Result).Type);
			Assert.AreEqual(eErrorType.Name, ((ExcelErrorValue)resultNAME.Result).Type);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)resultVALUE.Result).Type);
			Assert.AreEqual(eErrorType.Num, ((ExcelErrorValue)resultNUM.Result).Type);
			Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)resultDIV0.Result).Type);
			Assert.AreEqual(eErrorType.Ref, ((ExcelErrorValue)resultREF.Result).Type);
		}

		[TestMethod]
		public void AverageInWorksheetWithSingleInputsAsCellReferencesWorksAsExpected()
		{
			using (var package = new ExcelPackage())
			{
				var worksheet = package.Workbook.Worksheets.Add("Sheet1");
				worksheet.Cells["C2"].Value = 2;
				worksheet.Cells["C3"].Value = 2.5;
				worksheet.Cells["C4"].Value = "2";
				worksheet.Cells["C5"].Value = "word";
				worksheet.Cells["C6"].Value = "6/16/2017";
				worksheet.Cells["C7"].Value = true;
				worksheet.Cells["C8"].Value = null;
				worksheet.Cells["B2"].Formula = "AVERAGE(C2)";
				worksheet.Cells["B3"].Formula = "AVERAGE(C3)";
				worksheet.Cells["B4"].Formula = "AVERAGE(C4)";
				worksheet.Cells["B5"].Formula = "AVERAGE(C5)";
				worksheet.Cells["B6"].Formula = "AVERAGE(C6)";
				worksheet.Cells["B7"].Formula = "AVERAGE(C7)";
				worksheet.Cells["B8"].Formula = "AVERAGE(C8)";
				worksheet.Calculate();
				Assert.AreEqual(2d, worksheet.Cells["B2"].Value);
				Assert.AreEqual(2.5, worksheet.Cells["B3"].Value);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B4"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B5"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B6"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B7"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B8"].Value).Type);
			}
		}

		[TestMethod]
		public void AverageWithIntegerAndNumericStringReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs(1, "3");
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(2d, result.Result);
		}

		[TestMethod]
		public void AverageWithIntegerAndNonNumericStringReturnsPoundValue()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs(1, "word");
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void AverageWithIntegerAndBooleanValueReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs(3, true);
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(2d, result.Result);
		}

		[TestMethod]
		public void AverageWithIntegerAndNullArgumentReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs(1, null);
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(0.5, result.Result);
		}

		[TestMethod]
		public void AverageInWorksheetWithIntegerAndCellReferenceWorksAsExpected()
		{
			using (var package = new ExcelPackage())
			{
				var worksheet = package.Workbook.Worksheets.Add("Sheet1");
				worksheet.Cells["D2"].Formula = "YEARFRAC(,)"; // Evaluates to #NA.
				worksheet.Cells["C2"].Formula = "D2";
				worksheet.Cells["C3"].Value = "2";
				worksheet.Cells["C4"].Value = "word";
				worksheet.Cells["C5"].Value = true;
				worksheet.Cells["C6"].Value = null;
				worksheet.Cells["B2"].Formula = "C2";
				worksheet.Cells["B3"].Formula = "AVERAGE(1, C3)";
				worksheet.Cells["B4"].Formula = "AVERAGE(1, C4)";
				worksheet.Cells["B5"].Formula = "AVERAGE(1, C5)";
				worksheet.Cells["B6"].Formula = "AVERAGE(1, C6)";
				worksheet.Cells["B7"].Formula = "asdf"; // Evaluates to #NAME error.
				worksheet.Cells["B8"].Formula = "EDATE(-1,0)"; // Evaluates to #NUM error.
				worksheet.Calculate();
				Assert.AreEqual(eErrorType.NA, ((ExcelErrorValue)worksheet.Cells["B2"].Value).Type);
				Assert.AreEqual(1d, worksheet.Cells["B3"].Value);
				Assert.AreEqual(1d, worksheet.Cells["B4"].Value);
				Assert.AreEqual(1d, worksheet.Cells["B5"].Value);
				Assert.AreEqual(1d, worksheet.Cells["B6"].Value);
				Assert.AreEqual(eErrorType.Name, ((ExcelErrorValue)worksheet.Cells["B7"].Value).Type);
				Assert.AreEqual(eErrorType.Num, ((ExcelErrorValue)worksheet.Cells["B8"].Value).Type);
				
			}
		}

		[TestMethod]
		public void AverageWithNumericStringAndNonNumericStringReturnsPoundValue()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs("2", "word");
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void AverageWithNonNumericStringAndNumericStringReturnsPoundValue()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs("word", "2");
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void AverageWithBooleanValueAndNonNumericStringReturnsPoundValue()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs(true, "word");
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void AverageWithNonNumericStringAndBooleanValueReturnsPoundValue()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs("word", true);
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void AverageWithNumericStringAndBooleanValueReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs("2", true);
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(1.5, result.Result);
		}

		[TestMethod]
		public void AverageWithBooleanValueAndNumericStringReturnsCorrectResult()
		{
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs(true, "2");
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(1.5, result.Result);
		}

		[TestMethod]
		public void AverageInWorksheetWithOnlyNonNumericValuesInCellRangeReturnsPoundDiv0()
		{
			using (var package = new ExcelPackage())
			{
				var worksheet = package.Workbook.Worksheets.Add("Sheet1");
				worksheet.Cells["C2"].Value = "2";
				worksheet.Cells["C3"].Value = "word";
				worksheet.Cells["C4"].Value = true;
				worksheet.Cells["C5"].Value = "word";
				worksheet.Cells["C6"].Value = "2";
				worksheet.Cells["C7"].Value = true;
				worksheet.Cells["D2"].Value = "word";
				worksheet.Cells["D3"].Value = "2";
				worksheet.Cells["D4"].Value = "word";
				worksheet.Cells["D5"].Value = true;
				worksheet.Cells["D6"].Value = true;
				worksheet.Cells["D7"].Value = "2";
				worksheet.Cells["D8"].Formula = "YEARFRAC(,)"; // Evaluates to #NA.
				worksheet.Cells["B2"].Formula = "AVERAGE(C2:D2)";
				worksheet.Cells["B3"].Formula = "AVERAGE(C3:D3)";
				worksheet.Cells["B4"].Formula = "AVERAGE(C4:D4)";
				worksheet.Cells["B5"].Formula = "AVERAGE(C5:D5)";
				worksheet.Cells["B6"].Formula = "AVERAGE(C6:D6)";
				worksheet.Cells["B7"].Formula = "AVERAGE(C7:D7)";
				worksheet.Cells["B8"].Formula = "AVERAGE(D8)";
				worksheet.Calculate();
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B2"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B3"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B4"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B5"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B6"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B7"].Value).Type);
				Assert.AreEqual(eErrorType.NA, ((ExcelErrorValue)worksheet.Cells["B8"].Value).Type);
			}
		}

		[TestMethod]
		public void AverageInWorksheetWithArraysWorksAsExpected()
		{
			using (var package = new ExcelPackage())
			{
				var worksheet = package.Workbook.Worksheets.Add("Sheet1");
				worksheet.Cells["B2"].Formula = "AVERAGE({\"1\",\"2\",\"3\"})";
				worksheet.Cells["B3"].Formula = "AVERAGE(\"1\",\"2\",\"3\")";
				worksheet.Cells["B4"].Formula = "AVERAGE({1,2,3})";
				worksheet.Calculate();
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B2"].Value).Type);
				Assert.AreEqual(2d, worksheet.Cells["B3"].Value);
				Assert.AreEqual(2d, worksheet.Cells["B4"].Value);
			}
		}

		[TestMethod]
		public void AverageInWorksheetWithInputsAsValuesAndFormulasWorksAsExpected()
		{
			using (var package = new ExcelPackage())
			{
				var worksheet = package.Workbook.Worksheets.Add("Sheet1");
				worksheet.Cells["C2"].Value = 2;
				worksheet.Cells["C3"].Formula = "2";
				worksheet.Cells["C4"].Value = "2";
				worksheet.Cells["C5"].Formula = "\"2\"";
				worksheet.Cells["C6"].Value = true;
				worksheet.Cells["C7"].Formula = "TRUE";
				worksheet.Cells["C8"].Value = "6/20/2017";
				worksheet.Cells["C9"].Formula = "\"6/20/2017\"";
				worksheet.Cells["B2"].Formula = "AVERAGE(C2)";
				worksheet.Cells["B3"].Formula = "AVERAGE(C3)";
				worksheet.Cells["B4"].Formula = "AVERAGE(C4)";
				worksheet.Cells["B5"].Formula = "AVERAGE(C5)";
				worksheet.Cells["B6"].Formula = "AVERAGE(C6)";
				worksheet.Cells["B7"].Formula = "AVERAGE(C7)";
				worksheet.Cells["B8"].Formula = "AVERAGE(C8)";
				worksheet.Cells["B9"].Formula = "AVERAGE(C9)";
				worksheet.Calculate();
				Assert.AreEqual(2d, worksheet.Cells["B2"].Value);
				Assert.AreEqual(2d, worksheet.Cells["B3"].Value);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B4"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B5"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B6"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B7"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B8"].Value).Type);
				Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)worksheet.Cells["B9"].Value).Type);
			}
		}

		[TestMethod]
		public void AverageWithLiteralsWorksAsExpected()
		{
			// In the case of literals, Average DOES parse and include numeric strings, date strings, bools, etc.
			var date1 = new DateTime(2013, 1, 5);
			var date2 = new DateTime(2013, 1, 15);
			double[] expectedIndividualValues =
			{
				1000,
				2000,
				6000,
				1,
				date1.ToOADate(),
				date2.ToOADate()
			};
			var function = new Average();
			var arguments = FunctionsHelper.CreateArgs("1000", 2000, "6000", true, date1, date2.ToString("d"));
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(expectedIndividualValues.Average(), result.Result);
		}

		[TestMethod]
		public void AverageWithCellReferencesWorksAsExpected()
		{
			// In the case of cell references, Average DOES NOT parse and include numeric strings, date strings, bools, unparsable strings, etc.
			using (var package = new ExcelPackage())
			{
				var worksheet = package.Workbook.Worksheets.Add("Test");
				ExcelRange range1 = worksheet.Cells[1, 1];
				range1.Formula = "\"1000\"";
				range1.Calculate();
				var range2 = worksheet.Cells[1, 2];
				range2.Value = 2000;
				var range3 = worksheet.Cells[1, 3];
				range3.Formula = $"\"{new DateTime(2013, 1, 5).ToString("d")}\"";
				range3.Calculate();
				var range4 = worksheet.Cells[1, 4];
				range4.Value = true;
				var range5 = worksheet.Cells[1, 5];
				range5.Value = new DateTime(2013, 1, 5);
				var range6 = worksheet.Cells[1, 6];
				range6.Value = "Test";
				var rangeInfo1 = new EpplusExcelDataProvider.RangeInfo(worksheet, 1, 1, 1, 3);
				var rangeInfo2 = new EpplusExcelDataProvider.RangeInfo(worksheet, 1, 4, 1, 4);
				var rangeInfo3 = new EpplusExcelDataProvider.RangeInfo(worksheet, 1, 5, 1, 6);
				var address = new OfficeOpenXml.FormulaParsing.ExcelUtilities.RangeAddress();
				address.FromRow = address.ToRow = address.FromCol = address.ToCol = 2;
				this.ParsingContext.Scopes.NewScope(address);
				var function = new Average();
				var arguments = new FunctionArgument[]
				{
					new FunctionArgument(rangeInfo1),
					new FunctionArgument(rangeInfo2),
					new FunctionArgument(rangeInfo3)
				};
				var result = function.Execute(arguments, this.ParsingContext);
				Assert.AreEqual((2000 + new DateTime(2013, 1, 5).ToOADate()) / 2, result.Result);
			}
		}

		[TestMethod]
		public void AverageWithArraysWorksAsExpected()
		{
			// In the case of arrays, Average DOES NOT parse and include numeric strings, date strings, bools, unparsable strings, etc.
			var date1 = new DateTime(2013, 1, 5);
			var date2 = new DateTime(2013, 1, 15);
			var function = new Average();
			var argumentsInArray = new FunctionArgument[]
			{
				new FunctionArgument(1000.ToString("n")),
				new FunctionArgument(2000),
				new FunctionArgument(6000.ToString("n")),
				new FunctionArgument(true),
				new FunctionArgument(date1),
				new FunctionArgument(date2.ToString("d")),
				new FunctionArgument("test")
			};
			var argumentsInArrayInArray = new FunctionArgument[]
			{
				new FunctionArgument(argumentsInArray)
			};
			var result = function.Execute(argumentsInArrayInArray, this.ParsingContext);
			Assert.AreEqual((2000 + date1.ToOADate()) / 2, result.Result);
		}

		[TestMethod]
		public void AverageWithUnparsableLiteralWorksAsExpected()
		{
			// In the case of literals, any unparsable string literal results in a #VALUE.
			var function = new Average();
			var arguments = new FunctionArgument[]
			{
				new FunctionArgument(1000),
				new FunctionArgument("Test")
			};
			var result = function.Execute(arguments, this.ParsingContext);
			Assert.AreEqual(OfficeOpenXml.FormulaParsing.ExpressionGraph.DataType.ExcelError, result.DataType);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)(result.Result)).Type);
		}
		#endregion
	}
}
