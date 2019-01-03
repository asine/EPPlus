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
 * Jan Källman		Added		21-MAR-2011
 * Jan Källman		License changed GPL-->LGPL 2011-12-16
 *******************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using OfficeOpenXml.Extensions;
using OfficeOpenXml.Internationalization;
using OfficeOpenXml.Table.PivotTable.DataCalculation;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml.Table.PivotTable
{
	/// <summary>
	/// An Excel Pivottable
	/// </summary>
	public class ExcelPivotTable : XmlHelper
	{
		#region Constants
		private const string NamePath = "@name";
		private const string DisplayNamePath = "@displayName";
		private const string FirstHeaderRowPath = "d:location/@firstHeaderRow";
		private const string FirstDataRowPath = "d:location/@firstDataRow";
		private const string FirstDataColumnPath = "d:location/@firstDataCol";
		private const string StyleNamePath = "d:pivotTableStyleInfo/@name";
		#endregion

		#region Class Variables
		private ExcelPivotCacheDefinition myCacheDefinition;
		private ExcelPivotTableFieldCollection myFields;
		private ExcelPivotTableRowColumnFieldCollection myRowFields;
		private ExcelPivotTableRowColumnFieldCollection myColumnFields;
		private ExcelPivotTableDataFieldCollection myDataFields;
		private ExcelPivotTableRowColumnFieldCollection myPageFields;
		private ItemsCollection myRowItems;
		private ItemsCollection myColumnItems;
		private TableStyles myTableStyle = Table.TableStyles.Medium6;
		private ExcelAddress myAddress;
		#endregion

		#region Properties
		/// <summary>
		/// Gets the xml data representing the pivot table in the package.
		/// </summary>
		public XmlDocument PivotTableXml { get; private set; }

		/// <summary>
		/// Gets or sets the package internal URI to the pivot table xml Document.
		/// </summary>
		public Uri PivotTableUri { get; internal set; }

		/// <summary>
		/// Gets or sets the name of the pivot table object in Excel.
		/// </summary>
		public string Name
		{
			get
			{
				return base.GetXmlNodeString(NamePath);
			}
			set
			{
				if (this.Worksheet.Workbook.ExistsTableName(value))
					throw (new ArgumentException("PivotTable name is not unique"));
				string prevName = this.Name;
				if (this.Worksheet.Tables.TableNames.ContainsKey(prevName))
				{
					int ix = this.Worksheet.Tables.TableNames[prevName];
					this.Worksheet.Tables.TableNames.Remove(prevName);
					this.Worksheet.Tables.TableNames.Add(value, ix);
				}
				base.SetXmlNodeString(NamePath, value);
				base.SetXmlNodeString(DisplayNamePath, this.CleanDisplayName(value));
			}
		}

		/// <summary>
		/// Gets the reference to the pivot table cache definition object.
		/// </summary>
		public ExcelPivotCacheDefinition CacheDefinition
		{
			get
			{
				if (myCacheDefinition == null)
				{
					if (this.CacheDefinitionRelationship == null)
						throw new InvalidOperationException($"{nameof(this.CacheDefinitionRelationship)} is null.");

					var pivotTableCacheDefinitionPartName = UriHelper.GetUriEndTargetName(this.CacheDefinitionRelationship.TargetUri);
					foreach (var cacheDefinition in this.Worksheet.Workbook.PivotCacheDefinitions)
					{
						var cacheDefinitionPartName = UriHelper.GetUriEndTargetName(cacheDefinition.CacheDefinitionUri);
						if (pivotTableCacheDefinitionPartName.IsEquivalentTo(cacheDefinitionPartName))
						{
							myCacheDefinition = cacheDefinition;
							break;
						}
					}
				}
				return myCacheDefinition;
			}
			private set
			{
				myCacheDefinition = value;
			}
		}

		/// <summary>
		/// Gets the worksheet where the pivot table is located.
		/// </summary>
		public ExcelWorksheet Worksheet
		{
			get
			{
				return this.Workbook.Worksheets[this.Address.WorkSheet];
			}
		}

		/// <summary>
		/// Gets or sets the location of the pivot table.
		/// </summary>
		public ExcelAddress Address
		{
			get
			{
				return myAddress;
			}
			internal set
			{
				if (string.IsNullOrEmpty(value.WorkSheet))
					throw new InvalidOperationException("PivotTable address must specify a worsheet.");
				myAddress = value;
			}
		}

		/// <summary>
		/// Gets or sets whether multiple datafields are displayed in the row area or the column area.
		/// </summary>
		public bool DataOnRows
		{
			get
			{
				return base.GetXmlNodeBool("@dataOnRows");
			}
			set
			{
				base.SetXmlNodeBool("@dataOnRows", value);
			}
		}

		/// <summary>
		/// Gets or sets whether to apply the legacy table autoformat number format properties.
		/// </summary>
		public bool ApplyNumberFormats
		{
			get
			{
				return base.GetXmlNodeBool("@applyNumberFormats");
			}
			set
			{
				base.SetXmlNodeBool("@applyNumberFormats", value);
			}
		}

		/// <summary>
		/// Gets or sets whether to apply the legacy table autoformat border properties.
		/// </summary>
		public bool ApplyBorderFormats
		{
			get
			{
				return base.GetXmlNodeBool("@applyBorderFormats");
			}
			set
			{
				base.SetXmlNodeBool("@applyBorderFormats", value);
			}
		}

		/// <summary>
		/// Gets or sets whether to apply the legacy table autoformat font properties.
		/// </summary>
		public bool ApplyFontFormats
		{
			get
			{
				return base.GetXmlNodeBool("@applyFontFormats");
			}
			set
			{
				base.SetXmlNodeBool("@applyFontFormats", value);
			}
		}

		/// <summary>
		/// Gets or sets whether to apply the legacy table autoformat pattern properties.
		/// </summary>
		public bool ApplyPatternFormats
		{
			get
			{
				return base.GetXmlNodeBool("@applyPatternFormats");
			}
			set
			{
				base.SetXmlNodeBool("@applyPatternFormats", value);
			}
		}

		/// <summary>
		/// Gets or sets whether to apply the legacy table autoformat width/height properties.
		/// </summary>
		public bool ApplyWidthHeightFormats
		{
			get
			{
				return base.GetXmlNodeBool("@applyWidthHeightFormats");
			}
			set
			{
				base.SetXmlNodeBool("@applyWidthHeightFormats", value);
			}
		}

		/// <summary>
		/// Gets or sets whether to show member property information.
		/// </summary>
		public bool ShowMemberPropertyTips
		{
			get
			{
				return base.GetXmlNodeBool("@showMemberPropertyTips");
			}
			set
			{
				base.SetXmlNodeBool("@showMemberPropertyTips", value);
			}
		}

		/// <summary>
		/// Gets or sets whether to show the drill indicators.
		/// </summary>
		public bool ShowCalcMember
		{
			get
			{
				return base.GetXmlNodeBool("@showCalcMbrs");
			}
			set
			{
				base.SetXmlNodeBool("@showCalcMbrs", value);
			}
		}

		/// <summary>
		/// Gets or sets if the user can enable drill down on a PivotItem or aggregate value.
		/// </summary>
		public bool EnableDrill
		{
			get
			{
				return base.GetXmlNodeBool("@enableDrill", true);
			}
			set
			{
				base.SetXmlNodeBool("@enableDrill", value);
			}
		}

		/// <summary>
		/// Gets or sets whether to show the drill down buttons.
		/// </summary>
		public bool ShowDrill
		{
			get
			{
				return base.GetXmlNodeBool("@showDrill", true);
			}
			set
			{
				base.SetXmlNodeBool("@showDrill", value);
			}
		}

		/// <summary>
		/// Gets or sets whether the tooltips should be displayed for PivotTable data cells.
		/// </summary>
		public bool ShowDataTips
		{
			get
			{
				return base.GetXmlNodeBool("@showDataTips", true);
			}
			set
			{
				base.SetXmlNodeBool("@showDataTips", value, true);
			}
		}

		/// <summary>
		/// Gets or sets whether the row and column titles from the PivotTable should be printed.
		/// </summary>
		public bool FieldPrintTitles
		{
			get
			{
				return base.GetXmlNodeBool("@fieldPrintTitles");
			}
			set
			{
				base.SetXmlNodeBool("@fieldPrintTitles", value);
			}
		}

		/// <summary>
		/// Gets or sets whether the row and column titles from the PivotTable should be printed.
		/// </summary>
		public bool ItemPrintTitles
		{
			get
			{
				return base.GetXmlNodeBool("@itemPrintTitles");
			}
			set
			{
				base.SetXmlNodeBool("@itemPrintTitles", value);
			}
		}

		/// <summary>
		/// Gets or sets whether the grand totals should be displayed for the PivotTable columns.
		/// </summary>
		/// <remarks>A blank value in XML indicates true.</remarks>
		public bool ColumnGrandTotals
		{
			get
			{
				return base.GetXmlNodeBool("@colGrandTotals", true);
			}
			set
			{
				base.SetXmlNodeBool("@colGrandTotals", value);
			}
		}

		/// <summary>
		///Gets or sets whether the grand totals should be displayed for the PivotTable rows.
		/// </summary>
		/// <remarks>A blank value in XML indicates true.</remarks>
		public bool RowGrandTotals
		{
			get
			{
				return base.GetXmlNodeBool("@rowGrandTotals", true);
			}
			set
			{
				base.SetXmlNodeBool("@rowGrandTotals", value);
			}
		}

		/// <summary>
		/// Gets or sets whether the drill indicators expand collapse buttons should be printed.
		/// </summary>
		public bool PrintDrill
		{
			get
			{
				return base.GetXmlNodeBool("@printDrill");
			}
			set
			{
				base.SetXmlNodeBool("@printDrill", value);
			}
		}

		/// <summary>
		/// Gets or sets whether to show error messages in cells.
		/// </summary>
		public bool ShowError
		{
			get
			{
				return base.GetXmlNodeBool("@showError");
			}
			set
			{
				base.SetXmlNodeBool("@showError", value);
			}
		}

		/// <summary>
		/// Gets or sets the string to be displayed in cells that contain errors.
		/// </summary>
		public string ErrorCaption
		{
			get
			{
				return base.GetXmlNodeString("@errorCaption");
			}
			set
			{
				base.SetXmlNodeString("@errorCaption", value);
			}
		}

		/// <summary>
		/// Gets or sets the name of the value area field header in the PivotTable. 
		/// This caption is shown when the PivotTable when two or more fields are in the values area.
		/// </summary>
		public string DataCaption
		{
			get
			{
				return base.GetXmlNodeString("@dataCaption");
			}
			set
			{
				base.SetXmlNodeString("@dataCaption", value);
			}
		}

		/// <summary>
		/// Gets or sets whether to show field headers.
		/// </summary>
		public bool ShowHeaders
		{
			get
			{
				return base.GetXmlNodeBool("@showHeaders");
			}
			set
			{
				base.SetXmlNodeBool("@showHeaders", value);
			}
		}

		/// <summary>
		/// Gets or sets the number of page fields to display before starting another row or column.
		/// </summary>
		public int PageWrap
		{
			get
			{
				return base.GetXmlNodeInt("@pageWrap");
			}
			set
			{
				if (value < 0)
					throw new Exception("Value can't be negative");
				base.SetXmlNodeString("@pageWrap", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets whether the legacy auto formatting has been applied to the PivotTable view.
		/// </summary>
		public bool UseAutoFormatting
		{
			get
			{
				return base.GetXmlNodeBool("@useAutoFormatting");
			}
			set
			{
				base.SetXmlNodeBool("@useAutoFormatting", value);
			}
		}

		/// <summary>
		/// Gets or sets whether the in-grid drop zones should be displayed at runtime, and whether classic layout is applied.
		/// </summary>
		public bool GridDropZones
		{
			get
			{
				return base.GetXmlNodeBool("@gridDropZones");
			}
			set
			{
				base.SetXmlNodeBool("@gridDropZones", value);
			}
		}

		/// <summary>
		/// Gets or sets the indentation increment for compact axis or can be used to set the Report Layout to Compact Form.
		/// </summary>
		public int Indent
		{
			get
			{
				return base.GetXmlNodeInt("@indent");
			}
			set
			{
				base.SetXmlNodeString("@indent", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets whether data fields in the PivotTable should be displayed in outline form.
		/// </summary>
		public bool OutlineData
		{
			get
			{
				return base.GetXmlNodeBool("@outlineData");
			}
			set
			{
				base.SetXmlNodeBool("@outlineData", value);
			}
		}

		/// <summary>
		/// Gets or sets whether new fields should have their outline flag set to true.
		/// </summary>
		public bool Outline
		{
			get
			{
				return base.GetXmlNodeBool("@outline");
			}
			set
			{
				base.SetXmlNodeBool("@outline", value);
			}
		}

		/// <summary>
		/// Gets or sets whether the fields of a PivotTable can have multiple filters set on them.
		/// </summary>
		public bool MultipleFieldFilters
		{
			get
			{
				return base.GetXmlNodeBool("@multipleFieldFilters");
			}
			set
			{
				base.SetXmlNodeBool("@multipleFieldFilters", value);
			}
		}

		/// <summary>
		/// Gets or sets whether new fields should have their compact flag set to true.
		/// </summary>
		public bool Compact
		{
			get
			{
				return base.GetXmlNodeBool("@compact");
			}
			set
			{
				base.SetXmlNodeBool("@compact", value);
			}
		}

		/// <summary>
		/// Gets or sets whether the field next to the data field in the PivotTable should be displayed in the same column of the spreadsheet.
		/// </summary>
		public bool CompactData
		{
			get
			{
				return base.GetXmlNodeBool("@compactData");
			}
			set
			{
				base.SetXmlNodeBool("@compactData", value);
			}
		}

		/// <summary>
		/// Gets or sets the string to be displayed for grand totals.
		/// </summary>
		public string GrandTotalCaption
		{
			get
			{
				return base.GetXmlNodeString("@grandTotalCaption");
			}
			set
			{
				base.SetXmlNodeString("@grandTotalCaption", value);
			}
		}

		/// <summary>
		/// Gets or sets the string to be displayed in row header in compact mode.
		/// </summary>
		public string RowHeaderCaption
		{
			get
			{
				return base.GetXmlNodeString("@rowHeaderCaption");
			}
			set
			{
				base.SetXmlNodeString("@rowHeaderCaption", value);
			}
		}

		/// <summary>
		/// Gets or sets the string to be displayed in cells with no value.
		/// </summary>
		public string MissingCaption
		{
			get
			{
				return base.GetXmlNodeString("@missingCaption");
			}
			set
			{
				base.SetXmlNodeString("@missingCaption", value);
			}
		}

		/// <summary>
		/// Gets or sets the first row of the PivotTable header relative to the top left cell in the ref value.
		/// </summary>
		public int FirstHeaderRow
		{
			get
			{
				return base.GetXmlNodeInt(FirstHeaderRowPath);
			}
			set
			{
				base.SetXmlNodeString(FirstHeaderRowPath, value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the first column of the PivotTable data relative to the top left cell in the ref value.
		/// </summary>
		public int FirstDataRow
		{
			get
			{
				return base.GetXmlNodeInt(FirstDataRowPath);
			}
			set
			{
				base.SetXmlNodeString(FirstDataRowPath, value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the first column of the PivotTable data relative to the top left cell in the ref value.
		/// </summary>
		public int FirstDataCol
		{
			get
			{
				return base.GetXmlNodeInt(FirstDataColumnPath);
			}
			set
			{
				base.SetXmlNodeString(FirstDataColumnPath, value.ToString());
			}
		}

		/// <summary>
		/// Gets the fields in the table .
		/// </summary>
		public ExcelPivotTableFieldCollection Fields
		{
			get
			{
				if (myFields == null)
				{
					var pivotFieldsNode = this.TopNode.SelectSingleNode("d:pivotFields", this.NameSpaceManager);
					myFields = new ExcelPivotTableFieldCollection(this.NameSpaceManager, pivotFieldsNode, this);
				}
				return myFields;
			}
		}

		/// <summary>
		/// Gets the row label fields.
		/// </summary>
		public ExcelPivotTableRowColumnFieldCollection RowFields
		{
			get
			{
				if (myRowFields == null)
				{
					var rowFieldsNode = this.TopNode.SelectSingleNode("d:rowFields", this.NameSpaceManager);
					myRowFields = new ExcelPivotTableRowColumnFieldCollection(this.NameSpaceManager, rowFieldsNode, this, PivotTableItemType.Row);
				}
				return myRowFields;
			}
		}

		/// <summary>
		/// Gets the column label fields.
		/// </summary>
		public ExcelPivotTableRowColumnFieldCollection ColumnFields
		{
			get
			{
				if (myColumnFields == null)
				{
					var columnFieldsNode = this.TopNode.SelectSingleNode("d:colFields", this.NameSpaceManager);
					myColumnFields = new ExcelPivotTableRowColumnFieldCollection(this.NameSpaceManager, columnFieldsNode, this, PivotTableItemType.Column);
				}
				return myColumnFields;
			}
		}

		/// <summary>
		/// Gets the value fields.
		/// </summary>
		public ExcelPivotTableDataFieldCollection DataFields
		{
			get
			{
				if (myDataFields == null)
				{
					var dataFieldsNode = this.TopNode.SelectSingleNode("d:dataFields", this.NameSpaceManager);
					myDataFields = new ExcelPivotTableDataFieldCollection(this.NameSpaceManager, dataFieldsNode, this);
				}
				return myDataFields;
			}
		}

		/// <summary>
		/// Gets the report filter fields.
		/// </summary>
		public ExcelPivotTableRowColumnFieldCollection PageFields
		{
			get
			{
				if (myPageFields == null)
				{
					var pageFieldsNode = this.TopNode.SelectSingleNode("d:pageFields", this.NameSpaceManager);
					myPageFields = new ExcelPivotTableRowColumnFieldCollection(this.NameSpaceManager, pageFieldsNode, this, PivotTableItemType.Page);
				}
				return myPageFields;
			}
		}

		/// <summary>
		/// Gets the row items.
		/// </summary>
		public ItemsCollection RowItems
		{
			get
			{
				if (myRowItems == null)
					myRowItems = new ItemsCollection(this.NameSpaceManager, this.TopNode.SelectSingleNode("d:rowItems", this.NameSpaceManager));
				return myRowItems;
			}
		}

		/// <summary>
		/// Gets the column items.
		/// </summary>
		public ItemsCollection ColumnItems
		{
			get
			{
				if (myColumnItems == null)
					myColumnItems = new ItemsCollection(this.NameSpaceManager, this.TopNode.SelectSingleNode("d:colItems", this.NameSpaceManager));
				return myColumnItems;
			}
		}

		/// <summary>
		/// Gets or sets the pivot style name that is used for custom styles.
		/// </summary>
		public string StyleName
		{
			get
			{
				return base.GetXmlNodeString(StyleNamePath);
			}
			set
			{
				if (value.StartsWith("PivotStyle"))
				{
					try
					{
						myTableStyle = (TableStyles)Enum.Parse(typeof(TableStyles), value.Substring(10, value.Length - 10), true);
					}
					catch
					{
						myTableStyle = TableStyles.Custom;
					}
				}
				else if (value == "None")
				{
					myTableStyle = TableStyles.None;
					value = "";
				}
				else
					myTableStyle = TableStyles.Custom;
				base.SetXmlNodeString(StyleNamePath, value, true);
			}
		}

		/// <summary>
		/// Gets or sets the table style. If this is a custom property, the style from the StyleName propery is used.
		/// </summary>
		public TableStyles TableStyle
		{
			get
			{
				return myTableStyle;
			}
			set
			{
				myTableStyle = value;
				if (value != TableStyles.Custom)
					base.SetXmlNodeString(StyleNamePath, "PivotStyle" + value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the cache id of the pivot table.
		/// </summary>
		internal int CacheID
		{
			get
			{
				return base.GetXmlNodeInt("@cacheId");
			}
			set
			{
				base.SetXmlNodeString("@cacheId", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the pivot table part.
		/// </summary>
		internal Packaging.ZipPackagePart Part { get; set; }

		/// <summary>
		/// Gets or sets the worksheet-pivot table relationship.
		/// </summary>
		internal Packaging.ZipPackageRelationship WorksheetRelationship { get; set; }

		/// <summary>
		/// Gets or sets the cache definition-pivot table relationship.
		/// </summary>
		internal Packaging.ZipPackageRelationship CacheDefinitionRelationship { get; set; }

		/// <summary>
		/// Gets a list of pivot table row header cell models.
		/// </summary>
		internal List<PivotTableHeader> RowHeaders { get; } = new List<PivotTableHeader>();

		/// <summary>
		/// Gets a list of pivot table column header cell models.
		/// </summary>
		internal List<PivotTableHeader> ColumnHeaders { get; } = new List<PivotTableHeader>();

		/// <summary>
		/// Gets a value indicating whether there is more than one data field in the row fields.
		/// </summary>
		internal bool HasRowDataFields => this.RowFields.Any(c => c.Index == -2);

		/// <summary>
		/// Gets a value indicating whether there is more than one data field in the column fields.
		/// </summary>
		internal bool HasColumnDataFields => this.ColumnFields.Any(c => c.Index == -2);

		private ExcelWorkbook Workbook { get; set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates an instance of a <see cref="ExcelPivotTable"/> from a relationship.
		/// </summary>
		/// <param name="rel">The relationship to create the pivot table from.</param>
		/// <param name="sheet">The worksheet the pivot table is on.</param>
		internal ExcelPivotTable(Packaging.ZipPackageRelationship rel, ExcelWorksheet sheet) :
			 base(sheet.NameSpaceManager)
		{
			this.Workbook = sheet.Workbook;
			this.PivotTableUri = UriHelper.ResolvePartUri(rel.SourceUri, rel.TargetUri);
			this.WorksheetRelationship = rel;
			var pck = sheet.Package.Package;
			this.Part = pck.GetPart(this.PivotTableUri);

			this.PivotTableXml = new XmlDocument();
			XmlHelper.LoadXmlSafe(this.PivotTableXml, this.Part.GetStream());
			this.InitSchemaNodeOrder();
			this.TopNode = this.PivotTableXml.DocumentElement;
			this.Address = new ExcelAddress(sheet.Name, base.GetXmlNodeString("d:location/@ref"));

			var rels = this.Part.GetRelationshipsByType(ExcelPackage.schemaRelationships + "/pivotCacheDefinition");
			if (rels.Count != 1)
				throw new InvalidOperationException($"Pivot table had an unexpected number ({rels.Count}) of pivot cache definitions.");
			this.CacheDefinitionRelationship = rels.FirstOrDefault();

			this.LoadFields();
		}

		/// <summary>
		/// Creates an instance of a <see cref="ExcelPivotTable"/>.
		/// </summary>
		/// <param name="sheet">The worksheet of the pivot table.</param>
		/// <param name="address">The address of the pivot table.</param>
		/// <param name="sourceAddress">The address of the source data.</param>
		/// <param name="name">The name of the pivot table.</param>
		/// <param name="tblId">The pivot table id.</param>
		internal ExcelPivotTable(ExcelWorksheet sheet, ExcelAddress address, ExcelRangeBase sourceAddress, string name, int tblId) :
			 base(sheet.NameSpaceManager)
		{
			this.Workbook = sheet.Workbook;
			this.Address = new ExcelAddress(sheet.Name, address.Address);
			this.Address = address;
			var pck = sheet.Package.Package;

			this.PivotTableXml = new XmlDocument();
			LoadXmlSafe(this.PivotTableXml, this.GetStartXml(name, tblId, address, sourceAddress), Encoding.UTF8);
			this.TopNode = this.PivotTableXml.DocumentElement;
			this.PivotTableUri = GetNewUri(pck, "/xl/pivotTables/pivotTable{0}.xml", ref tblId);
			this.InitSchemaNodeOrder();

			this.Part = pck.CreatePart(this.PivotTableUri, ExcelPackage.schemaPivotTable);
			this.PivotTableXml.Save(this.Part.GetStream());

			// Worksheet-PivotTable relationship
			this.WorksheetRelationship = sheet.Part.CreateRelationship(UriHelper.ResolvePartUri(sheet.WorksheetUri, this.PivotTableUri), Packaging.TargetMode.Internal, ExcelPackage.schemaRelationships + "/pivotTable");
			bool cacheDefinitionFound = false;
			foreach (var cache in this.Worksheet.Workbook.PivotCacheDefinitions)
			{
				if (cache.GetSourceRangeAddress().IsEquivalentRange(sourceAddress))
				{
					this.CacheDefinition = cache;
					cacheDefinitionFound = true;
					break;
				}
			}
			if (!cacheDefinitionFound)
			{
				this.CacheDefinition = new ExcelPivotCacheDefinition(sheet.NameSpaceManager, this, sourceAddress, tblId);
				sheet.Workbook.PivotCacheDefinitions.Add(this.CacheDefinition);
			}
			// CacheDefinition-PivotTable relationship
			this.CacheDefinitionRelationship = this.Part.CreateRelationship(UriHelper.ResolvePartUri(this.PivotTableUri, this.CacheDefinition.CacheDefinitionUri), Packaging.TargetMode.Internal, ExcelPackage.schemaRelationships + "/pivotCacheDefinition");
			sheet.Workbook.AddPivotTable(this.CacheID.ToString(), this.CacheDefinition.CacheDefinitionUri);
			this.LoadFields();
			using (var range = sheet.Cells[address.Address])
			{
				range.Clear();
			}
		}
		#endregion

		#region Internal Methods
		/// <summary>
		/// Refresh the <see cref="ExcelPivotTable"/> based on the <see cref="ExcelPivotCacheDefinition"/>.
		/// </summary>
		internal void RefreshFromCache(StringResources stringResources)
		{
			// Update pivotField items to match corresponding cacheField sharedItems.
			foreach (var pivotField in this.Fields)
			{
				var fieldItems = pivotField.Items;
				var sharedItemsCount = this.CacheDefinition.CacheFields[pivotField.Index].SharedItems.Count;

				if (fieldItems.Count > sharedItemsCount + 1)
					throw new InvalidOperationException("There are more pivotField items than cacheField sharedItems.");

				if (fieldItems.Count > 0)
				{
					fieldItems.Clear(pivotField.DefaultSubtotal);
					var sharedItemsList = this.CacheDefinition.CacheFields[pivotField.Index].SharedItems.ToList();
					// Sort the items alphabetically/numerically.
					var sortedList = sharedItemsList.OrderBy(x => x.Value);
					// Sort the items chronologically.
					if (pivotField.Name.IsEquivalentTo("Month"))
						sortedList = sharedItemsList.OrderBy(m => DateTime.ParseExact(m.Value, "MMMMM", new CultureInfo("en-US")));
					// Assign the correct index value to each item.
					for (int i = 0; i < sortedList.Count(); i++)
					{
						int index = sharedItemsList.FindIndex(x => x == sortedList.ElementAt(i));
						fieldItems.AddItem(i, index, pivotField.DefaultSubtotal);
					}
				}
			}

			// Update the rowItems.
			this.UpdateRowColumnItems(this.RowFields, this.RowItems, true);

			// Update the colItems.
			this.UpdateRowColumnItems(this.ColumnFields, this.ColumnItems, false);

			// Update the pivot table data.
			this.UpdateWorksheet(stringResources);

			// Remove the 'm' (missing) xml attribute from each pivot field item, if it exists, to prevent 
			// corrupting the workbook, since Excel automatically adds them.
			this.RemovePivotFieldItemMAttribute();
		}
		#endregion

		#region Private Methods
		private void RemovePivotFieldItemMAttribute()
		{
			foreach (var pivotField in this.Fields)
			{
				if (pivotField.Items.Count > 0)
				{
					foreach (var item in pivotField.Items)
					{
						var mAttribute = item.TopNode.Attributes["m"];
						if (mAttribute != null && int.Parse(mAttribute.Value) == 1)
							item.TopNode.Attributes.Remove(mAttribute);
					}
				}
			}
		}

		private void UpdateRowColumnItems(ExcelPivotTableRowColumnFieldCollection field, ItemsCollection collection, bool isRowItems)
		{
			// Update the rowItems.
			if (field.Any())
			{
				collection.Clear();
				if (isRowItems)
					this.BuildRowItems(0, new List<Tuple<int, int>>(), 0);
				else
					this.BuildColumnItems(0, new List<Tuple<int, int>>(), false, 0);
				// Create grand total items if necessary.
				bool grandTotals = isRowItems ? this.RowGrandTotals : this.ColumnGrandTotals;
				if (grandTotals && isRowItems && !(this.RowFields.Count == 1 && this.RowFields.First().Index == -2))
					this.CreateTotalNodes("grand", true, null, null, 0, false, this.HasRowDataFields);
				else if (grandTotals && !isRowItems && !(this.ColumnFields.Count == 1 && this.ColumnFields.First().Index == -2))
					this.CreateTotalNodes("grand", false, null, null, 0, false, this.HasColumnDataFields);
			}
			else
			{
				string xmlTag = isRowItems ? "d:rowFields" : "d:colFields";
				// If there are no row/column fields, then remove tag or else it will corrupt the workbook.
				this.TopNode.RemoveChild(this.TopNode.SelectSingleNode(xmlTag, this.NameSpaceManager));
				var headerCollection = isRowItems ? this.RowHeaders : this.ColumnHeaders;
				headerCollection.Add(new PivotTableHeader(null, null, 0, false, false, false, false));
			}
		}

		private void CreateTotalNodes(string itemType, bool isRowItem, List<Tuple<int, int>> indices, ExcelPivotTableField pivotField, int repeatedItemsCount, bool multipleDataFields, bool hasDataFields)
		{
			var itemsCollection = isRowItem ? this.RowItems : this.ColumnItems;
			var headerCollection = isRowItem ? this.RowHeaders : this.ColumnHeaders;

			// Variables are set for the default case where item type is a grand total.
			int index = this.DataFields.Count > 0 && hasDataFields ? this.DataFields.Count : 1;
			int xMember = 0;
			bool aboveDataField = false;
			bool grandTotal = true;

			// Reset variables if item type is a default total.
			if (itemType.IsEquivalentTo("default"))
			{
				index = multipleDataFields ? this.DataFields.Count : 1;
				xMember = indices.Last().Item2;
				aboveDataField = isRowItem ? true : !indices.Any(x => x.Item1 == -2);
				grandTotal = false;
			}

			// Create the xml node and row/column header.
			for (int i = 0; i < index; i++)
			{
				var header = new PivotTableHeader(indices, pivotField, i, grandTotal, isRowItem, false, false, itemType, aboveDataField);
				itemsCollection.AddSumNode(itemType, repeatedItemsCount, xMember, i);
				headerCollection.Add(header);
			}
		}

		private void BuildRowItems(int rowDepth, List<Tuple<int, int>> parentNodeIndices, int dataFieldIndex)
		{
			// Base case.
			if (rowDepth >= this.RowFields.Count)
				return;

			var pivotFieldIndex = this.RowFields[rowDepth].Index;

			// Initialize local variables and the default case is a pivot table with multiple row data fields (pivotFieldIndex == -2).
			ExcelPivotTableField pivotField = null;
			int maxIndex = this.DataFields.Count;
			bool isAboveDataField = false;
			bool isDataField = true;
			// If the pivotFieldIndex is not a data field index, then set the variables accordingly.
			this.SetNonDataFieldVariables(pivotFieldIndex, parentNodeIndices, ref pivotField, ref maxIndex, ref isAboveDataField, ref isDataField);

			// Create xml nodes and row headers.
			for (int i = 0; i < maxIndex; i++)
			{
				var childList = parentNodeIndices.ToList();
				childList.Add(new Tuple<int, int>(pivotFieldIndex, i));
				bool leafNode = rowDepth == this.RowFields.Count - 1;
				int myDataFieldIndex = pivotFieldIndex == -2 ? i : dataFieldIndex;
				if (pivotField == null || this.CacheDefinition.CacheRecords.Contains(childList))
				{
					this.RowItems.Add(rowDepth, i, null, myDataFieldIndex);
					this.RowHeaders.Add(new PivotTableHeader(childList, pivotField, myDataFieldIndex, false, true, leafNode, isDataField, null, isAboveDataField));
					this.BuildRowItems(rowDepth + 1, childList, myDataFieldIndex);
				}
			}

			// Get the last pivot field to check if subtotals are used.
			if (pivotFieldIndex == -2 && parentNodeIndices.Count > 0 && parentNodeIndices.Last().Item1 != -2)
				pivotField = this.Fields[parentNodeIndices.Last().Item1];
			if (parentNodeIndices.Any() && parentNodeIndices.Last().Item1 != -2 && pivotField.DefaultSubtotal)
			{
				var hasDataFieldParent = parentNodeIndices.Any(x => x.Item1 == -2);
				int repeatedItemsCount = rowDepth - 1;

				// Last row field is a datafield, there are no column fields and we are not at a leaf node.
				if (rowDepth != this.RowFields.Count - 1 && this.RowFields.Last().Index == -2 && this.ColumnFields.Count == 0)
					this.CreateTotalNodes("default", true, parentNodeIndices, pivotField, repeatedItemsCount, true, this.HasRowDataFields);
				// If there are multiple data fields, then create a subtotal node for each data field. Otherwise, only create one subtotal node.
				else if (rowDepth != this.RowFields.Count - 1 && (!pivotField.SubtotalTop && !hasDataFieldParent))
					this.CreateTotalNodes("default", true, parentNodeIndices, pivotField, repeatedItemsCount, true, this.HasRowDataFields);
				else if (!pivotField.SubtotalTop && (hasDataFieldParent || this.DataFields.Count == 1))
					this.CreateTotalNodes("default", true, parentNodeIndices, pivotField, repeatedItemsCount, false, this.HasRowDataFields);
			}
		}

		private bool BuildColumnItems(int colDepth, List<Tuple<int, int>> parentNodeIndices, bool itemsCreated, int dataFieldIndex)
		{
			if (colDepth >= this.ColumnFields.Count)
				return true;

			var pivotFieldIndex = this.ColumnFields[colDepth].Index;

			// Variables are set to the default case of a pivot table with multiple column data fields (pivotFieldIndex == -2).
			ExcelPivotTableField pivotField = null;
			int maxIndex = this.DataFields.Count;
			bool isAboveDataField = false;
			bool isDataField = true;
			// If the pivotFieldIndex is not a data field index, then set the variables accordingly.
			this.SetNonDataFieldVariables(pivotFieldIndex, parentNodeIndices, ref pivotField, ref maxIndex, ref isAboveDataField, ref isDataField);

			// Create xml nodes and column headers.
			this.CreateColumnItemNode(maxIndex, parentNodeIndices, pivotFieldIndex, pivotField, dataFieldIndex, colDepth, itemsCreated, isDataField, isAboveDataField);

			// Get the last pivot field to check if subtotals are used.
			if (pivotFieldIndex == -2 && parentNodeIndices.Count > 0 && parentNodeIndices.Last().Item1 != -2)
				pivotField = this.Fields[parentNodeIndices.Last().Item1];
			if (pivotField != null && pivotField.DefaultSubtotal && parentNodeIndices.Any() && parentNodeIndices.Last().Item1 != -2)
			{
				bool hasDataFieldParent = parentNodeIndices.Any(x => x.Item1 == -2);
				int rValue = itemsCreated ? colDepth - 1 : colDepth;
				int rAttribute = rValue == colDepth ? rValue - 1 : rValue;
				bool isLastNonDataField = this.ColumnFields.Skip(rAttribute + 1).All(x => x.Index == -2);
				parentNodeIndices = this.FindIndices(parentNodeIndices);
				// If the node is above a data field node and there are multiple data fields, then create a subtotal node for each data field. 
				if (this.DataFields.Count > 0 && !hasDataFieldParent && !isLastNonDataField && this.HasColumnDataFields)
					this.CreateTotalNodes("default", false, parentNodeIndices, pivotField, rAttribute, true, this.HasColumnDataFields);
				// Otherwise, if the node is not the last non-data field node and is below a data field node, then only create one subtotal node.
				else if (!isLastNonDataField && (!isAboveDataField || !this.HasColumnDataFields))
					this.CreateTotalNodes("default", false, parentNodeIndices, pivotField, rAttribute, false, this.HasColumnDataFields);
			}

			return itemsCreated;
		}

		private void SetNonDataFieldVariables(int pivotFieldIndex, List<Tuple<int, int>> indices, ref ExcelPivotTableField pivotField, ref int maxIndex, ref bool isAboveDataField, ref bool isDataField)
		{
			if (pivotFieldIndex != -2)
			{
				pivotField = this.Fields[pivotFieldIndex];
				maxIndex = pivotField.DefaultSubtotal ? pivotField.Items.Count - 1 : pivotField.Items.Count;
				isAboveDataField = !indices.Any(x => x.Item1 == -2);
				isDataField = false;
			}
		}

		private void CreateColumnItemNode(int index, List<Tuple<int, int>> indices, int pivotFieldIndex, ExcelPivotTableField pivotField, int dataFieldIndex, 
			int colDepth, bool itemsCreated, bool isDataField, bool isAboveDataField)
		{
			for (int i = 0; i < index; i++)
			{
				var childList = indices.ToList();
				int itemIndex = pivotFieldIndex == -2 ? i : pivotField.Items[i].X;
				childList.Add(new Tuple<int, int>(pivotFieldIndex, itemIndex));
				if (this.CacheDefinition.CacheRecords.Contains(childList))
				{
					int myDataFieldIndex = pivotFieldIndex == -2 ? i : dataFieldIndex;
					bool result = this.BuildColumnItems(colDepth + 1, childList, itemsCreated, myDataFieldIndex);
					if (colDepth == this.ColumnFields.Count - 1)
					{
						int repeatedItemsCount = 0;
						// Convert the second value in the tuple to the index in the list.
						childList = this.FindIndices(childList);
						// Find the value of the repeated items count.
						if (this.ColumnItems.Count > 0)
						{
							// Compare current column item node indices to previous to find the index of the differing xNode.
							var lastColumnHeader = this.ColumnHeaders.Last();
							for (int j = 0; j < childList.Count; j++)
							{
								if (lastColumnHeader.CacheRecordIndices[j].Item2 != childList[j].Item2)
								{
									repeatedItemsCount = j;
									break;
								}
							}
						}
						this.ColumnHeaders.Add(new PivotTableHeader(childList.ToList(), pivotField, myDataFieldIndex, false, false, true, isDataField, null, isAboveDataField));
						this.ColumnItems.AddColumnItem(childList, repeatedItemsCount, myDataFieldIndex);
						itemsCreated = true;
					}
					else if (colDepth == 0)
						itemsCreated = false;
					else if (colDepth < this.ColumnFields.Count - 1)
						itemsCreated = result;
				}
			}
		}

		private List<Tuple<int, int>> FindIndices(List<Tuple<int, int>> indices)
		{
			for (int i = 0; i < indices.Count; i++)
			{
				var pivotField = this.Fields[indices[i].Item1];
				var index = pivotField.Items.ToList().FindIndex(x => x.X == indices[i].Item2);
				indices[i] = new Tuple<int, int>(indices[i].Item1, index);
			}
			return indices;
		}

		private void UpdateWorksheet(StringResources stringResources)
		{
			this.UpdateRowColumnHeaders(stringResources);

			// Update the pivot table's address.
			int endRow = this.Address.Start.Row + this.FirstDataRow + this.RowHeaders.Count - 1;
			// If there are no data fields, then don't find the offset to obtain the first data column.
			int endColumn = this.DataFields.Any() ? this.Address.Start.Column + this.FirstDataCol + this.ColumnHeaders.Count - 1 
				: this.Address.Start.Column;
			this.Address = new ExcelAddress(this.Worksheet.Name, this.Address.Start.Row, this.Address.Start.Column, endRow, endColumn);
			
			if (this.DataFields.Any())
			{
				var backingTableData = this.WritePivotTableBodyData();
				List<object>[] grandTotalsValuesLists = null;
				RowGrandTotalHelper rowGrandTotalHelper = null;
				ColumnGrandTotalHelper columnGrandTotalHelper = null;
				if (this.ColumnGrandTotals)
				{
					columnGrandTotalHelper = new ColumnGrandTotalHelper(this, backingTableData);
					grandTotalsValuesLists = columnGrandTotalHelper.UpdateGrandTotals();
				}
				if (this.RowGrandTotals)
				{
					rowGrandTotalHelper = new RowGrandTotalHelper(this, backingTableData);
					rowGrandTotalHelper.UpdateGrandTotals();
				}
				// Write grand-grand totals to worksheet (grand totals at bottom right corner of pivot table).
				if (this.ColumnGrandTotals && this.RowGrandTotals && this.ColumnFields.Any())
				{
					if (this.HasRowDataFields)
						rowGrandTotalHelper.UpdateGrandGrandTotals(grandTotalsValuesLists);
					else
						columnGrandTotalHelper.UpdateGrandGrandTotals(grandTotalsValuesLists);
				}
			}
			else
			{
				// If there are no data fields, then remove the xml node to prevent corrupting the workbook.
				this.TopNode.RemoveChild(this.TopNode.SelectSingleNode("d:dataFields", this.NameSpaceManager));
			}
		}

		private void UpdateRowColumnHeaders(StringResources stringResources)
		{
			// Clear out the pivot table in the worksheet.
			int startRow = this.Address.Start.Row + this.FirstHeaderRow;
			int headerColumn = this.Address.Start.Column + this.FirstDataCol;
			int dataRow = this.Address.Start.Row + this.FirstDataRow;
			this.Worksheet.Cells[dataRow, this.Address.Start.Column, this.Address.End.Row, this.Address.Start.Column].Clear();
			this.Worksheet.Cells[startRow, headerColumn, this.Address.End.Row, this.Address.End.Column].Clear();

			// Update the row headers in the worksheet.
			if (this.RowFields.Any())
			{
				for (int i = 0; i < this.RowItems.Count; i++)
				{
					bool itemType = this.SetTotalCellValue(this.RowFields, this.RowItems[i], this.RowHeaders[i], dataRow, this.Address.Start.Column, stringResources);
					if (itemType)
					{
						dataRow++;
						continue;
					}
					var sharedItem = this.GetSharedItemValue(this.RowFields, this.RowItems[i], this.RowItems[i].RepeatedItemsCount, 0);
					this.Worksheet.Cells[dataRow++, this.Address.Start.Column].Value = sharedItem;
				}
			}
			// If there are no row headers and only one data field, print the name of the data field for the row.
			else if (this.DataFields.Count == 1)
				this.Worksheet.Cells[dataRow++, this.Address.Start.Column].Value = this.DataFields.First().Name;

			// Update the column headers in the worksheet.
			if (this.ColumnFields.Any())
			{
				for (int i = 0; i < this.ColumnItems.Count; i++)
				{
					int startHeaderRow = startRow;
					bool itemType = this.SetTotalCellValue(this.ColumnFields, this.ColumnItems[i], this.ColumnHeaders[i], startHeaderRow, headerColumn, stringResources);
					if (itemType)
					{
						headerColumn++;
						continue;
					}

					for (int j = 0; j < this.ColumnItems[i].Count; j++)
					{
						var columnFieldIndex = this.ColumnItems[i].RepeatedItemsCount == 0 ? j : j + this.ColumnItems[i].RepeatedItemsCount;
						var sharedItem = this.GetSharedItemValue(this.ColumnFields, this.ColumnItems[i], columnFieldIndex, j);
						var cellRow = this.ColumnItems[i].RepeatedItemsCount == 0 ? startHeaderRow : startHeaderRow + this.ColumnItems[i].RepeatedItemsCount;
						this.Worksheet.Cells[cellRow, headerColumn].Value = sharedItem;
						startHeaderRow++;
					}
					headerColumn++;
				}
			}
			// If there are no column headers and only one data field, print the name of the data field for the column.
			else if (this.DataFields.Count == 1)
				this.Worksheet.Cells[this.Address.Start.Row, headerColumn].Value = this.DataFields.First().Name;
		}

		private List<object>[,] WritePivotTableBodyData()
		{
			var backingData = new List<object>[this.RowHeaders.Count(), this.ColumnHeaders.Count()];
			int dataColumn = this.Address.Start.Column + this.FirstDataCol;
			using (var totalsCalculator = new TotalsFunctionHelper())
			{
				for (int column = 0; column < this.ColumnHeaders.Count; column++)
				{
					var columnHeader = this.ColumnHeaders[column];
					int dataRow = this.Address.Start.Row + this.FirstDataRow - 1;
					for (int row = 0; row < this.RowHeaders.Count; row++)
					{
						dataRow++;
						var rowHeader = this.RowHeaders[row];
						if (rowHeader.IsGrandTotal || columnHeader.IsGrandTotal)
							continue;
						if ((rowHeader.CacheRecordIndices == null && columnHeader.CacheRecordIndices.Count == this.ColumnFields.Count)
							|| rowHeader.CacheRecordIndices.Count == this.RowFields.Count)
						{
							// At a leaf node.
							backingData[row, column] = this.GetBackingCellValues(rowHeader, columnHeader);
						}
						else if (this.HasRowDataFields)
						{
							if (rowHeader.PivotTableField != null && rowHeader.PivotTableField.DefaultSubtotal)
							{
								if ((rowHeader.PivotTableField != null && rowHeader.PivotTableField.SubtotalTop && !rowHeader.IsAboveDataField) 
									|| rowHeader.SumType.IsEquivalentTo("default"))
								{
									backingData[row, column] = this.GetBackingCellValues(rowHeader, columnHeader);
								}
							}
						}
						else if (rowHeader.PivotTableField.DefaultSubtotal && (rowHeader.SumType != null || rowHeader.PivotTableField.SubtotalTop))
							backingData[row, column] = this.GetBackingCellValues(rowHeader, columnHeader);

						if (backingData[row, column] != null)
							this.WriteCellResult(dataRow, dataColumn, rowHeader, columnHeader, this.HasRowDataFields, totalsCalculator);
					}
						dataColumn++;
				}
			}
			return backingData;
		}

		private List<object> GetBackingCellValues(PivotTableHeader rowHeader, PivotTableHeader columnHeader)
		{
			var dataFieldCollectionIndex = this.HasRowDataFields ? rowHeader.DataFieldCollectionIndex : columnHeader.DataFieldCollectionIndex;
			var dataField = this.DataFields[dataFieldCollectionIndex];
			return this.CacheDefinition.CacheRecords.FindMatchingValues(
				this,
				rowHeader.CacheRecordIndices,
				columnHeader.CacheRecordIndices,
				dataField.Index);
		}

		private void WriteCellResult(int row, int column, PivotTableHeader rowHeader, PivotTableHeader columnHeader, bool hasRowDataFields, TotalsFunctionHelper functionCalculator)
		{
			var dataFieldCollectionIndex = this.HasRowDataFields ? rowHeader.DataFieldCollectionIndex : columnHeader.DataFieldCollectionIndex;
			var dataField = this.DataFields[dataFieldCollectionIndex];
			var matchingValues = this.CacheDefinition.CacheRecords.FindMatchingValues(
				this,
				rowHeader.CacheRecordIndices,
				columnHeader.CacheRecordIndices,
				dataField.Index);
			this.WriteCellTotal(row, column, dataField, matchingValues, functionCalculator);
		}

		private void WriteCellTotal(int row, int column, ExcelPivotTableDataField dataField, List<object> values, TotalsFunctionHelper functionCalculator)
		{
			var cell = this.Worksheet.Cells[row, column];
			cell.Value = functionCalculator.Calculate(dataField.Function, values);
			var style = this.Worksheet.Workbook.Styles.NumberFormats.FirstOrDefault(n => n.NumFmtId == dataField.NumFmtId);
			if (style != null)
				cell.Style.Numberformat.Format = style.Format;
		}

		private bool SetTotalCellValue(ExcelPivotTableRowColumnFieldCollection field, RowColumnItem item, PivotTableHeader header, int row, int column, StringResources stringResources)
		{
			if (!string.IsNullOrEmpty(item.ItemType))
			{
				// If the field is a row field, then use the given row number. 
				// Otherwise, calculate the correct row number for column fields.
				int rowLabel = field == this.RowFields ? row : row + item.RepeatedItemsCount;
				if (item.ItemType.IsEquivalentTo("grand"))
				{
					// If the pivot table has more than one data field, then use the name of the data field in the total.
					if ((this.HasRowDataFields && field == this.RowFields) || (this.HasColumnDataFields && field == this.ColumnFields))
					{
						string dataFieldName = this.DataFields[item.DataFieldIndex].Name;
						this.Worksheet.Cells[rowLabel, column].Value = string.Format(stringResources.TotalCaptionWithFollowingValue, dataFieldName);
					}
					else
						this.Worksheet.Cells[rowLabel, column].Value = stringResources.GrandTotalCaption;
				}
				else if (item.ItemType.IsEquivalentTo("default"))
				{
					var itemName = this.GetSharedItemValue(field, item, item.RepeatedItemsCount, 0);
					if (this.DataFields.Count > 1 && header.IsAboveDataField && 
						((this.HasRowDataFields && field == this.RowFields) || (this.HasColumnDataFields && field == this.ColumnFields)))
					{
						string dataFieldName = this.DataFields[item.DataFieldIndex].Name;
						this.Worksheet.Cells[rowLabel, column].Value = $"{itemName} {dataFieldName}";
					}
					else
						this.Worksheet.Cells[rowLabel, column].Value = string.Format(stringResources.TotalCaptionWithPrecedingValue, itemName);
				}
				return true;
			}
			return false;
		}

		private string GetSharedItemValue(ExcelPivotTableRowColumnFieldCollection field, RowColumnItem item, int repeatedItemsCount, int xMemberIndex)
		{
			var pivotFieldIndex = field[repeatedItemsCount].Index;
			// A field that has an 'x' attribute equal to -2 is a special row/column field that indicates the
			// pivot table has more than one data field. Excel uses this to display the headings for the data 
			// values and how to group them in relation to other rows/columns. 
			// If a special field alrady exists in that collection, then another one will not be generated.
			if (pivotFieldIndex == -2)
				return this.DataFields[item.DataFieldIndex].Name;
			var pivotField = this.Fields[pivotFieldIndex];
			var cacheItemIndex = pivotField.Items[item[xMemberIndex]].X;
			return this.CacheDefinition.CacheFields[pivotFieldIndex].SharedItems[cacheItemIndex].Value;
		}

		private void InitSchemaNodeOrder()
		{
			this.SchemaNodeOrder = new string[] { "location", "pivotFields", "rowFields", "rowItems", "colFields", "colItems", "pageFields", "pageItems", "dataFields", "dataItems", "formats", "pivotTableStyleInfo" };
		}

		private void LoadFields()
		{
			// Add fields.
			int index = 0;
			var fieldNodes = this.CacheDefinition.TopNode.SelectNodes("d:cacheFields/d:cacheField", this.NameSpaceManager);
			if (fieldNodes != null)
			{
				foreach (var pivotField in this.Fields)
				{
					pivotField.SetCacheFieldNode(fieldNodes[index++]);
				}
			}
		}

		private string GetStartXml(string name, int id, ExcelAddress address, ExcelAddress sourceAddress)
		{
			string xml = string.Format("<pivotTableDefinition xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" name=\"{0}\" cacheId=\"{1}\" dataOnRows=\"1\" applyNumberFormats=\"0\" applyBorderFormats=\"0\" applyFontFormats=\"0\" applyPatternFormats=\"0\" applyAlignmentFormats=\"0\" applyWidthHeightFormats=\"1\" dataCaption=\"Data\"  createdVersion=\"4\" showMemberPropertyTips=\"0\" useAutoFormatting=\"1\" itemPrintTitles=\"1\" indent=\"0\" compact=\"0\" compactData=\"0\" gridDropZones=\"1\">", name, id);

			xml += string.Format("<location ref=\"{0}\" firstHeaderRow=\"1\" firstDataRow=\"1\" firstDataCol=\"1\" /> ", address.FirstAddress);
			xml += string.Format("<pivotFields count=\"{0}\">", sourceAddress._toCol - sourceAddress._fromCol + 1);
			for (int col = sourceAddress._fromCol; col <= sourceAddress._toCol; col++)
			{
				xml += "<pivotField showAll=\"0\" />";
			}

			xml += "</pivotFields>";
			xml += "<pivotTableStyleInfo name=\"PivotStyleMedium9\" showRowHeaders=\"1\" showColHeaders=\"1\" showRowStripes=\"0\" showColStripes=\"0\" showLastColumn=\"1\" />";
			xml += "</pivotTableDefinition>";
			return xml;
		}

		private string CleanDisplayName(string name)
		{
			return Regex.Replace(name, @"[^\w\.-_]", "_");
		}
		#endregion
	}
}