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
using System.Xml;
using OfficeOpenXml.Table.PivotTable;

namespace OfficeOpenXml.Drawing.Chart
{
	/// <summary>
	/// Provides access to line chart specific properties
	/// </summary>
	public class ExcelLineChart : ExcelChart
	{
		#region Constants
		private const string MarkerPath = "c:marker/@val";
		private const string SmoothPath = "c:smooth/@val";
		#endregion
		
		#region Class Variables
		private ExcelChartDataLabel myDataLabel = null;
		#endregion

		#region Constructors
		internal ExcelLineChart(ExcelDrawings drawings, XmlNode node, Uri uriChart, Packaging.ZipPackagePart part, XmlDocument chartXml, XmlNode chartNode) :
			 base(drawings, node, uriChart, part, chartXml, chartNode)
		{
		}

		internal ExcelLineChart(ExcelChart topChart, XmlNode chartNode) :
			 base(topChart, chartNode)
		{
		}

		internal ExcelLineChart(ExcelDrawings drawings, XmlNode node, eChartType type, ExcelChart topChart, ExcelPivotTable PivotTableSource) :
			 base(drawings, node, type, topChart, PivotTableSource)
		{
			this.Smooth = false;
		}
		#endregion

		#region Properties
		/// <summary>
		/// If the series has markers
		/// </summary>
		public bool Marker
		{
			get
			{
				return this.ChartXmlHelper.GetXmlNodeBool(ExcelLineChart.MarkerPath, false);
			}
			set
			{
				this.ChartXmlHelper.SetXmlNodeBool(ExcelLineChart.MarkerPath, value, false);
			}
		}

		/// <summary>
		/// If the series has smooth lines
		/// </summary>
		public bool Smooth
		{
			get
			{
				return this.ChartXmlHelper.GetXmlNodeBool(ExcelLineChart.SmoothPath, false);
			}
			set
			{
				this.ChartXmlHelper.SetXmlNodeBool(ExcelLineChart.SmoothPath, value);
			}
		}

		/// <summary>
		/// Access to datalabel properties
		/// </summary>
		public ExcelChartDataLabel DataLabel
		{
			get
			{
				if (this.myDataLabel == null)
					this.myDataLabel = new ExcelChartDataLabel(this.NameSpaceManager, this.ChartNode);
				return this.myDataLabel;
			}
		}
		#endregion

		#region Internal Methods
		internal override eChartType GetChartType(string name)
		{
			if (name == "lineChart")
			{
				if (this.ChartXmlHelper == null)
					return eChartType.Line;
				if (this.Marker)
				{
					if (this.Grouping == eGrouping.Stacked)
						return eChartType.LineMarkersStacked;
					else if (this.Grouping == eGrouping.PercentStacked)
						return eChartType.LineMarkersStacked100;
					else
						return eChartType.LineMarkers;
				}
				else
				{
					if (this.Grouping == eGrouping.Stacked)
						return eChartType.LineStacked;
					else if (this.Grouping == eGrouping.PercentStacked)
						return eChartType.LineStacked100;
					else
						return eChartType.Line;
				}
			}
			else if (name == "line3DChart")
				return eChartType.Line3D;
			return base.GetChartType(name);
		}
		#endregion
	}
}
