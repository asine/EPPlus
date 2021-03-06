﻿/*******************************************************************************
 * You may amend and distribute as you like, but don't remove this header!
 *
 * EPPlus provides server-side generation of Excel 2007/2010 spreadsheets.
 * See http://www.codeplex.com/EPPlus for details.
 *
 * Copyright (C) 2011  Jan Källman
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
 * ******************************************************************************
 * Jan Källman		Initial Release		        2009-10-01
 * Jan Källman		License changed GPL-->LGPL 2011-12-16
 *******************************************************************************/
using System;
using System.Globalization;
using System.Xml;
using OfficeOpenXml.Table.PivotTable;

namespace OfficeOpenXml.Drawing.Chart
{
	/// <summary>
	/// Provides access to ofpie-chart specific properties
	/// </summary>
	public class ExcelOfPieChart : ExcelPieChart
	{
		#region Constants
		private const string PieTypePath = "c:ofPieType/@val";
		private const string GapWidthPath = "c:gapWidth/@val";
		#endregion

		#region Properties
		/// <summary>
		/// Type, pie or bar
		/// </summary>
		public ePieType OfPieType
		{
			get
			{
				if (this.ChartXmlHelper.GetXmlNodeString(PieTypePath) == "bar")
					return ePieType.Bar;
				else
					return ePieType.Pie;
			}
			internal set
			{
				this.ChartXmlHelper.CreateNode(ExcelOfPieChart.PieTypePath, true);
				this.ChartXmlHelper.SetXmlNodeString(ExcelOfPieChart.PieTypePath, value == ePieType.Bar ? "bar" : "pie");
			}
		}

		/// <summary>
		/// The size of the gap between two adjacent bars/columns
		/// </summary>
		public int GapWidth
		{
			get
			{
				return this.ChartXmlHelper.GetXmlNodeInt(ExcelOfPieChart.GapWidthPath);
			}
			set
			{
				this.ChartXmlHelper.SetXmlNodeString(ExcelOfPieChart.GapWidthPath, value.ToString(CultureInfo.InvariantCulture));
			}
		}
		#endregion

		#region Constructors
		internal ExcelOfPieChart(ExcelDrawings drawings, XmlNode node, eChartType type, bool isPivot) :
			 base(drawings, node, type, isPivot)
		{
			SetTypeProperties();
		}
		internal ExcelOfPieChart(ExcelDrawings drawings, XmlNode node, eChartType type, ExcelChart topChart, ExcelPivotTable PivotTableSource) :
			 base(drawings, node, type, topChart, PivotTableSource)
		{
			SetTypeProperties();
		}

		internal ExcelOfPieChart(ExcelDrawings drawings, XmlNode node, Uri uriChart, Packaging.ZipPackagePart part, XmlDocument chartXml, XmlNode chartNode) :
			base(drawings, node, uriChart, part, chartXml, chartNode)
		{
			SetTypeProperties();
		}
		#endregion

		#region Private Methods
		private void SetTypeProperties()
		{
			if (this.ChartType == eChartType.BarOfPie)
				this.OfPieType = ePieType.Bar;
			else
				this.OfPieType = ePieType.Pie;
		}
		#endregion

		#region Internal Methods
		internal override eChartType GetChartType(string name)
		{
			if (name == "ofPieChart")
			{
				if (this.OfPieType == ePieType.Bar)
					return eChartType.BarOfPie;
				else
					return eChartType.PieOfPie;
			}
			return base.GetChartType(name);
		}
		#endregion
	}
}
