﻿/* Copyright (C) 2011  Jan Källman
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
 * Code change notes:
 * 
 * Author							Change						Date
 *******************************************************************************
 * Mats Alm   		                Added		                2013-12-03
 *******************************************************************************/
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.ExcelUtilities;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup
{
	public abstract class LookupFunction : ExcelFunction
	{
		private readonly ValueMatcher _valueMatcher;
		private readonly CompileResultFactory _compileResultFactory;

		/// <summary>
		/// Gets or sets a value representing the indicies of the arguments to the lookup function that
		/// should be compiled as ExcelAddresses instead of being evaluated.
		/// </summary>
		public List<int> LookupArgumentIndicies { get; set; }

		public LookupFunction()
			 : this(new LookupValueMatcher(), new CompileResultFactory())
		{

		}

		public LookupFunction(ValueMatcher valueMatcher, CompileResultFactory compileResultFactory)
		{
			_valueMatcher = valueMatcher;
			_compileResultFactory = compileResultFactory;
		}

		protected int IsMatch(object o1, object o2)
		{
			return _valueMatcher.IsMatch(o1, o2);
		}

		protected LookupDirection GetLookupDirection(RangeAddress rangeAddress)
		{
			var nRows = rangeAddress.ToRow - rangeAddress.FromRow;
			var nCols = rangeAddress.ToCol - rangeAddress.FromCol;
			return nCols > nRows ? LookupDirection.Horizontal : LookupDirection.Vertical;
		}

		protected CompileResult Lookup(LookupNavigator navigator, LookupArguments lookupArgs)
		{
			object lastValue = null;
			object lastLookupValue = null;
			int? lastMatchResult = null;
			if (lookupArgs.SearchedValue == null)
			{
				return new CompileResult(eErrorType.NA);
			}
			do
			{
				var matchResult = IsMatch(navigator.CurrentValue, lookupArgs.SearchedValue);
				if (matchResult != 0)
				{
					if (lastValue != null && navigator.CurrentValue == null) break;

					if (!lookupArgs.RangeLookup) continue;
					if (lastValue == null && matchResult > 0)
					{
						return new CompileResult(eErrorType.NA);
					}
					if (lastValue != null && matchResult > 0 && lastMatchResult < 0)
					{
						return _compileResultFactory.Create(lastLookupValue);
					}
					lastMatchResult = matchResult;
					lastValue = navigator.CurrentValue;
					lastLookupValue = navigator.GetLookupValue();
				}
				else
				{
					return _compileResultFactory.Create(navigator.GetLookupValue());
				}
			}
			while (navigator.MoveNext());

			return lookupArgs.RangeLookup ? _compileResultFactory.Create(lastLookupValue) : new CompileResult(eErrorType.NA);
		}

		protected ExcelAddress CalculateOffset(FunctionArgument[] arguments, ParsingContext context)
		{
			var startRange = ArgToString(arguments, 0);
			var rowOffset = ArgToInt(arguments, 1);
			var columnOffset = ArgToInt(arguments, 2);
			int width = 0, height = 0;
			if (arguments.Length > 3)
				height = ArgToInt(arguments, 3);
			if (arguments.Length > 4)
				width = ArgToInt(arguments, 4);
			if ((arguments.Length > 3 && height == 0) || (arguments.Length > 4 && width == 0))
				return null;
			var address = new ExcelAddress(startRange);
			string targetWorksheetName;
			if (string.IsNullOrEmpty(address.WorkSheet))
				targetWorksheetName = context.Scopes?.Current?.Address?.Worksheet;
			else
				targetWorksheetName = address.WorkSheet;
			var fromRow = address._fromRow + rowOffset;
			var fromCol = address._fromCol + columnOffset;
			var toRow = (height != 0 ? height : address._toRow) + rowOffset;
			var toCol = (width != 0 ? width : address._toCol) + columnOffset;
			return new ExcelAddress(targetWorksheetName, fromRow, fromCol, toRow, toCol);
		}
	}
}
