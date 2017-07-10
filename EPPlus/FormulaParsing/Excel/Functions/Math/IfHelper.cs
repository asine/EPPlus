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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OfficeOpenXml.FormulaParsing.Excel.Operators;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.Math
{
	/// <summary>
	/// This class provides a criteria comparison function to use in any Excel functions
	/// that require comparing cell values against a specific criteria.
	/// This class is currently used in AverageIf.cs, AverageIfs.cs, SumIf.cs, SumIfs.cs, and CountIf.cs.
	/// </summary>
	public static class IfHelper
	{
		/// <summary>
		/// Compares the given <paramref name="objectToCompare"/> against the given <paramref name="criteria"/>.
		/// This method is expected to be used with any of the *IF or *IFS Excel functions (ex: the AVERAGEIF function).
		/// </summary>
		/// <param name="objectToCompare">The object to compare against the given <paramref name="criteria"/>.</param>
		/// <param name="criteria">The criteria value or expression that dictates whether the given <paramref name="objectToCompare"/> passes or fails.</param>
		/// <returns>Returns true if <paramref name="objectToCompare"/> matches the <paramref name="criteria"/>.</returns>
		public static bool ObjectMatchesCriteria(object objectToCompare, string criteria)
		{
			var operatorIndex = -1;
			// Check if the criteria is an expression; i.e. begins with the operators <>, =, >, >=, <, or <=
			if (Regex.IsMatch(criteria, @"^(<>|>=|<=){1}"))
				operatorIndex = 2;
			else if (Regex.IsMatch(criteria, @"^(=|<|>){1}"))
				operatorIndex = 1;
			// If the criteria is an expression, evaluate as such.
			if (operatorIndex != -1)
			{
				var expressionOperatorString = criteria.Substring(0, operatorIndex);
				var criteriaString = criteria.Substring(operatorIndex);
				IOperator expressionOperator;
				if (OperatorsDict.Instance.TryGetValue(expressionOperatorString, out expressionOperator))
				{
					switch (expressionOperator.Operator)
					{
						case OperatorType.Equals:
							return IsMatch(objectToCompare, criteriaString, true);
						case OperatorType.NotEqualTo:
							return !IsMatch(objectToCompare, criteriaString, true);
						case OperatorType.GreaterThan:
						case OperatorType.GreaterThanOrEqual:
						case OperatorType.LessThan:
						case OperatorType.LessThanOrEqual:
							return CompareAsInequalityExpression(objectToCompare, criteriaString, expressionOperator.Operator);
						default:
							return IsMatch(objectToCompare, criteriaString);
					}
				}
			}
			return IsMatch(objectToCompare, criteria);
		}

		/// <summary>
		/// Compares the <paramref name="objectToCompare"/> with the given <paramref name="criteria"/>.
		/// <paramref name="criteria"/> is expected to be either a number, boolean, string, or null. The string
		/// can contain a date/time, or a text value that may require wildcard Regex.
		/// The given object is considered a match with the criteria if their content are equivalent in value.
		/// </summary>
		/// <param name="objectToCompare">The object to compare against the given <paramref name="criteria"/>.</param>
		/// <param name="criteria">The criteria value that dictates whether the <paramref name="objectToCompare"/> passes or fails.</param>
		/// <param name="matchAsEqualityExpression">
		///		Indicate if the <paramref name="criteria"/> came from an equality related expression,
		///		which requires slightly different handling.</param>
		/// <returns>Returns true if <paramref name="objectToCompare"/> matches the <paramref name="criteria"/>.</returns>
		private static bool IsMatch(object objectToCompare, string criteria, bool matchAsEqualityExpression = false)
		{
			// Equality related expression evaluation (= or <>) only considers empty cells as equal to empty string criteria.
			// If the given criteria was not originally preceded by an equality operator, then 
			// both empty cells and cells containing the empty string are considered as equal to empty string criteria.
			if (criteria.Equals(string.Empty))
				return ((matchAsEqualityExpression) ? (objectToCompare == null) : (objectToCompare == null || objectToCompare.Equals(string.Empty)));
			var criteriaIsBool = criteria.Equals(Boolean.TrueString.ToUpper()) || criteria.Equals(Boolean.FalseString.ToUpper());
			if (ConvertUtil.TryParseDateObjectToOADate(criteria, out double criteriaAsOADate))
				criteria = criteriaAsOADate.ToString();
			string objectAsString = null;
			if (objectToCompare == null)
				return false;
			else if (objectToCompare is bool && criteriaIsBool)
				return criteria.Equals(objectToCompare.ToString().ToUpper());
			else if (objectToCompare is bool ^ criteriaIsBool)
				return false;
			else if (ConvertUtil.TryParseDateObjectToOADate(objectToCompare, out double objectAsOADate))
				objectAsString = objectAsOADate.ToString();
			else
				objectAsString = objectToCompare.ToString().ToUpper();
			if (criteria.Contains("*") || criteria.Contains("?"))
			{
				var regexPattern = Regex.Escape(criteria);
				regexPattern = string.Format("^{0}$", regexPattern);
				regexPattern = regexPattern.Replace(@"\*", ".*");
				regexPattern = regexPattern.Replace("~.*", "\\*");
				regexPattern = regexPattern.Replace(@"\?", ".");
				regexPattern = regexPattern.Replace("~.", "\\?");
				return Regex.IsMatch(objectAsString, regexPattern);
			}
			else
				// A return value of 0 from CompareTo means that the two strings have equivalent content.
				return (criteria.CompareTo(objectAsString) == 0);
		}

		/// <summary>
		/// Compare the given <paramref name="objectToCompare"/> with the given <paramref name="criteria"/> using
		/// the given <paramref name="comparisonOperator"/>.
		/// </summary>
		/// <param name="objectToCompare">The object to compare against the given <paramref name="criteria"/>.</param>
		/// <param name="criteria">The criteria value that dictates whether the <paramref name="objectToCompare"/> passes or fails.</param>
		/// <param name="comparisonOperator">
		///		The inequality operator that dictates how the <paramref name="objectToCompare"/> should
		///		be compared to the <paramref name="criteria"/>.</param>
		/// <returns>Returns true if the <paramref name="objectToCompare"/> passes the comparison with <paramref name="criteria"/>.</returns>
		private static bool CompareAsInequalityExpression(object objectToCompare, string criteria, OperatorType comparisonOperator)
		{
			if (objectToCompare == null || objectToCompare is ExcelErrorValue)
				return false;
			var comparisonResult = int.MinValue;
			if (ConvertUtil.TryParseDateObjectToOADate(criteria, out double criteriaNumber)) // Handle the criteria as a number/date.
			{
				if (IsNumeric(objectToCompare, true))
				{
					var numberToCompare = ConvertUtil.GetValueDouble(objectToCompare);
					comparisonResult = numberToCompare.CompareTo(criteriaNumber);
				}
				else
					return false;
			}
			else // Handle the criteria as a non-numeric, non-date text value.
			{
				if (criteria.Equals(Boolean.TrueString.ToUpper()) || criteria.Equals(Boolean.FalseString.ToUpper()))
				{
					if (!(objectToCompare is bool objectBool))
						return false;
				}
				else if (IsNumeric(objectToCompare))
					return false;
				comparisonResult = (objectToCompare.ToString().ToUpper()).CompareTo(criteria);
			}
			switch (comparisonOperator)
			{
				case OperatorType.LessThan:
					return (comparisonResult == -1);
				case OperatorType.LessThanOrEqual:
					return (comparisonResult == -1 || comparisonResult == 0);
				case OperatorType.GreaterThan:
					return (comparisonResult == 1);
				case OperatorType.GreaterThanOrEqual:
					return (comparisonResult == 1 || comparisonResult == 0);
				default:
					throw new InvalidOperationException(
						"The default condition is invalid because this function should only be called if the given operator is <,<=,>, or >=.");
			}
		}

		/// <summary>
		/// Returns true if <paramref name="numericCandidate"/> is numeric.
		/// </summary>
		/// <param name="numericCandidate">The object to check for numeric content.</param>
		/// <param name="excludeBool">
		///		An optional parameter to exclude boolean values from the data types that are considered numeric.
		///		This method considers booleans as numeric by default.</param>
		/// <returns>Returns true if <paramref name="numericCandidate"/> is numeric.</returns>
		public static bool IsNumeric(object numericCandidate, bool excludeBool = false)
		{
			if (numericCandidate == null)
				return false;
			if (excludeBool && numericCandidate is bool)
				return false;
			return (numericCandidate.GetType().IsPrimitive || 
				numericCandidate is double || 
				numericCandidate is decimal || 
				numericCandidate is System.DateTime || 
				numericCandidate is TimeSpan);
		}

		/// <summary>
		/// Ensures that the given <paramref name="criteriaCandidate"/> is of a form that can be
		/// represented as a criteria.
		/// </summary>
		/// <param name="criteriaCandidate">The <see cref="FunctionArgument"/> containing the criteria.</param>
		/// <param name="context">The context from the function calling this function.</param>
		/// <returns>Returns the criteria in <paramref name="criteriaCandidate"/> as a string.</returns>
		public static string ExtractCriteriaString(FunctionArgument criteriaCandidate, ParsingContext context)
		{
			object criteriaObject = null;
			if (criteriaCandidate.Value is ExcelDataProvider.IRangeInfo criteriaRange)
			{
				if (criteriaRange.IsMulti)
				{
					var worksheet = context.ExcelDataProvider.GetRange(context.Scopes.Current.Address.Worksheet, 1, 1, "A1").Worksheet;
					var functionRow = context.Scopes.Current.Address.FromRow;
					var functionColumn = context.Scopes.Current.Address.FromCol;
					criteriaObject = CalculateCriteria(criteriaCandidate, worksheet, functionRow, functionColumn);
				}
				else
				{
					criteriaObject = criteriaCandidate.ValueFirst;
					if (criteriaObject is List<object> objectList)
						criteriaObject = objectList.First();
				}
			}
			else if (criteriaCandidate.Value is List<FunctionArgument> argumentList)
				criteriaObject = argumentList.First().ValueFirst;
			else
				criteriaObject = criteriaCandidate.ValueFirst;

			// Note that Excel considers null criteria equivalent to a criteria of 0.
			if (criteriaObject == null)
				return "0";
			else
				return criteriaObject.ToString().ToUpper();
		}

		public static object CalculateCriteria(FunctionArgument criteriaArgument, ExcelWorksheet worksheet, int rowLocation, int colLocation)
		{
			if (criteriaArgument.Value == null)
				return 0;
			if (criteriaArgument.Value is ExcelErrorValue)
				if (worksheet == null)
					return 0;
			if (rowLocation <= 0 || colLocation <= 0)
				return 0;

			var criteriaCandidate = criteriaArgument.ValueAsRangeInfo.Address;

			if (criteriaCandidate.Rows > criteriaCandidate.Columns)
			{
				var currentAddressRow = rowLocation;
				var startRow = criteriaCandidate.Start.Row;
				var endRow = criteriaCandidate.End.Row;

				if (currentAddressRow == startRow)
				{
					var cellColumn = criteriaCandidate.Start.Column;
					return worksheet.Cells[startRow, cellColumn].Value;
				}
				else if (currentAddressRow == endRow)
				{
					var cellColumn = criteriaCandidate.Start.Column;
					return worksheet.Cells[endRow, cellColumn].Value;
				}
				else if (currentAddressRow > startRow && currentAddressRow < endRow)
				{

					var cellColumn = criteriaCandidate.Start.Column;
					return worksheet.Cells[currentAddressRow, cellColumn].Value;
				}
				else
					return 0;
			}
			else if (criteriaCandidate.Rows < criteriaCandidate.Columns)
			{
				var currentAddressCol = colLocation;
				var startCol = criteriaCandidate.Start.Column;
				var endCol = criteriaCandidate.End.Column;

				if (currentAddressCol == startCol)
				{
					var cellRow = criteriaCandidate.Start.Row;
					return worksheet.Cells[cellRow, currentAddressCol].Value;
				}
				else if (currentAddressCol == endCol)
				{
					var cellRow = criteriaCandidate.Start.Row;
					return worksheet.Cells[cellRow, currentAddressCol].Value;
				}
				else if (currentAddressCol > startCol && currentAddressCol < endCol)
				{
					var cellRow = criteriaCandidate.Start.Row;
					return worksheet.Cells[cellRow, currentAddressCol].Value;
				}
				else
					return 0;
			}
			else
				return 0;
		}

		/// <summary>
		/// Takes a cell range and converts it into a single value criteria
		/// </summary>
		/// <param name="arguments">The cell range that will be reduced to a single value criteria.</param>
		/// <param name="worksheet">The current worksheet that is being used.</param>
		/// <param name="rowLocation">The row location of the cell that is calling this function.</param>
		/// <param name="colLocation">The column location of the cell that is calling this function.</param>
		/// <returns>A single value criteria as an integer.</returns>
		public static object CalculateCriteria(IEnumerable<FunctionArgument> arguments, ExcelWorksheet worksheet, int rowLocation, int colLocation)
		{
			if (arguments.ElementAt(1).Value == null)
				return 0;
			if (arguments.ElementAt(1).Value is ExcelErrorValue)
			if (worksheet == null)
				return 0;
			if (rowLocation <= 0 || colLocation <= 0)
				return 0;

			var criteriaCandidate = arguments.ElementAt(1).ValueAsRangeInfo.Address;

			if (criteriaCandidate.Rows > criteriaCandidate.Columns)
			{
				var currentAddressRow = rowLocation;
				var startRow = criteriaCandidate.Start.Row;
				var endRow = criteriaCandidate.End.Row;

				if (currentAddressRow == startRow)
				{
					var cellColumn = criteriaCandidate.Start.Column;
					return worksheet.Cells[startRow, cellColumn].Value;
				}
				else if (currentAddressRow == endRow)
				{
					var cellColumn = criteriaCandidate.Start.Column;
					return worksheet.Cells[endRow, cellColumn].Value;
				}
				else if (currentAddressRow > startRow && currentAddressRow < endRow)
				{

					var cellColumn = criteriaCandidate.Start.Column;
					return worksheet.Cells[currentAddressRow, cellColumn].Value;
				}
				else
					return 0;
			}
			else if (criteriaCandidate.Rows < criteriaCandidate.Columns)
			{
				var currentAddressCol = colLocation;
				var startCol = criteriaCandidate.Start.Column;
				var endCol = criteriaCandidate.End.Column;

				if (currentAddressCol == startCol)
				{
					var cellRow = criteriaCandidate.Start.Row;
					return worksheet.Cells[cellRow, currentAddressCol].Value;
				}
				else if (currentAddressCol == endCol)
				{
					var cellRow = criteriaCandidate.Start.Row;
					return worksheet.Cells[cellRow, currentAddressCol].Value;
				}
				else if (currentAddressCol > startCol && currentAddressCol < endCol)
				{
					var cellRow = criteriaCandidate.Start.Row;
					return worksheet.Cells[cellRow, currentAddressCol].Value;
				}
				else
					return 0;
			}
			else
				return 0;
		}
	}
}
