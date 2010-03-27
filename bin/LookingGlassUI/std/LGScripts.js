// Various common JS routines written for LookingGlass
// Copyright 2009, Robert Adams
// Licensed under BSD licence 
//          (http://www.opensource.org/licenses/bsd-license.php)

// ===========================================
// Build a string for a table to hold the passed data.
// We presume the data is in the form:
//   {rowname: { col1: val, col2: val; ...}, ...)
// 'sect' is the section of the document to build the table in
// 'data' is the data to put in as  the table
// 'addRowName' is 'true' if to add an initial column with the rowname
// 'rebuild' is 'true' if to rebuild the  table every time called
//     otherwise the table is built once and reused in later calles
// The table built has IDs and classes added to every cell so
// one can put data into the cell and format the cells and columns.
// If a table for 'SECT' is built and the data has 'row' and 'col'
// entries:
//   "rowA": { "colA": "dataAA", "colB":"dataAB", ...},
//   "rowB": { "colA": "dataBA", "colB":"dataBB", ...}, ...
// then the table built has"
// |table id='SECT-table' class='SECT-table-class'|
// |tr| |th class="SECT-rowName-header"| |th class="SECT-COLX-header"| ... |/tr|
// |tr| |td id="SECT-rowName-ROWX" class="SECT--class"| |/td|
//      |td id="SECT-ROWX-COLX" class="SECT-COLX-class"| |/td|
//      ...
// |/tr|
// ... 
// |/table|
//
// Function takes three additional arguments:
//     addRowName: add a header row of the column names (default=true)
//     addDisplayCol: add a last column named display (default=false)
//     columns: an array of column names to display. If empty, build
//       the column names from the data
function BuildTableForData(sectID, tableID, data, addRowName, addDisplayCol, columns) {
    var L = 0;
    for (var K in columns) { L++; }
    if (L == 0) {
        // build an array of all the column names
        for (row in data) {
            for (col in data[row]) {
                if (columns[col] == undefined) {
                    columns[col] = col;
                }
                
            }
        }
    }
    if (addDisplayCol) {
        columns['Display'] = 'Display';
    }
    // create a table with td's with id's for cell addressing
    var buff = new StringBuffer();
    var tableClass = MakeID(sectID + "-table-class")
    buff.append('<table id="' + tableID + '" class="' + tableClass +'">');
    buff.append('<tr>');
    var headerClass = MakeID(sectID + '-rowName-header');
    if (addRowName) buff.append('<th class="' + headerClass + '"></th>');
    for (col in columns) {
        headerClass = MakeID(sectID + '-' + columns[col] + '-header');
        buff.append('<th class="' + headerClass +'">' + columns[col] + '</th>');
    }
    buff.append('</tr>');
    for (row in data) {
        buff.append('<tr>');
        if (addRowName) {
            var rowID=MakeID(sectID + '-rowName-' + row);
            var rowClass = MakeID(sectID + '--class');
            buff.append('<td id="' + rowID + '" class="' + rowClass + '">.</td>');
        }
        for (col in columns) {
            var cellID = MakeID(sectID + '-' + row + '-' + col);
            var cellClass = MakeID(sectID + '-' + col + '-class');
            buff.append('<td id="' + cellID + '" class="' + cellClass + '">.</td>');
        }
        buff.append('</tr>');
    }
    buff.append('</table>');
    return buff.toString();
}

// ===========================================
// table in the specified section.
// Clear whatever is there and refill it with the data.
function BuildBasicTable(sect, data /*, addRowName, rebuild, addDisplayCol*/) {
    var sectID = MakeID(sect.substr(1))
    var tableID = MakeID(sectID + "-table")
    var addRowName = true;
    var rebuild = false;
    var addDisplayCol = false;
    if (arguments.length > 2) addRowName = arguments[2];
    if (arguments.length > 3) rebuild = arguments[3];
    if (arguments.length > 4) addDisplayCol = arguments[4];
    var specifyColumns = new Array();
    if (arguments.length > 5) specifyColumns = arguments[5];
    if ($('#' + tableID).length == 0 || rebuild) {
        // table does not exist. Build same
        $(sect).empty();
        $(sect).append(BuildTableForData(sectID, tableID, data, addRowName, addDisplayCol, specifyColumns));
    }
    // Fill its cells with the text data
    for (row in data) {
        $('#' + MakeID(sectID + '-rowName-' + row)).text(row);
        for (col in data[row]) {
            var cellID = MakeID(sectID + '-' + row + '-' + col);
            if ($('#' + cellID).length != 0) {
                $('#' + cellID).text(data[row][col]);
            }
        }
    }
}
// ===========================================
// do a table and specify which columns to display
// Note that column names are passed in as a list.
function BuildColumnTable(sect, data, addRow, rebuild, addDisp, columns) {
    var colSpec = new Array();
    for (col in columns) {
        colSpec[columns[col]] = columns[col];
    }
    BuildBasicTable(sect, data, addRow, rebuild, addDisp, colSpec);
}

// clean up ID so there are no dots
function MakeID(inID) {
    return inID.replace(/\./g, '-');
}
// Appendable string
function StringBuffer() {
    this.__strings__ = new Array;
}
StringBuffer.prototype.append = function(str) {
    this.__strings__.push(str);
}
StringBuffer.prototype.toString = function() {
    return this.__strings__.join("");
}
// ===========================================
// Class for keeping and displaying trending data.
// A new instance of the class is created and instance.AddPoint(pnt)
// is called to add data points. A sequence of up to
// instance.maxDataPoints (default 100) are collected with old
// being thrown away.
// The display is put into the page by calling instance.InsertDisplay(id)
// where id is an HTML element id to put the code. After placing the
// code, the display will be automatically updated.
// Formatting is done via formatting params passed at creation.
// Set format before inserting the display html.
// Look at the default example below for the options.
// Uses 'sparklines' so you must include that script library.
TrendData.prototype.maxDataPoints = 100;
TrendData.prototype.dataPoints = [];
function TrendData(numberOfPoints) {
    this.maxDataPoints = numberOfPoints;
    for (var ii=0; ii<this.maxDataPoints; ii++) this.dataPoints[ii] = 0;
}
TrendData.prototype.AddPoint = function(pnt) {
    for (var ii=this.maxDataPoints-1; ii>0; ii--) {
        this.dataPoints[ii] = this.dataPoints[ii-1];
    }
    this.dataPoints[0] = pnt;
}
TrendData.prototype.UpdateDisplay = function(id) {
    $.sparkline_display_visible();
    $(id).sparkline(this.dataPoints, this.formatParams);
    
}
TrendData.prototype.Format = function(format) {
    this.formatParams = format;
}
TrendData.prototype.formatParms =
    {type: 'line', // line (default), bar, tristate, discrete, bullet, pie or box
     width: 'auto',      // 'auto' or any css width spec
     height: 'auto',     // 'auto' or any valid css height spec
     lineColor: 'black', // Used by line and discrete charts
     // chartRangeMin: '0', // min value for range, default to min value
     // chardRangeMax: '0', // max value for range, default to max value
     // composite: 'true',  // true to overwrite existing chart (chart on chart)
     fillColor: 'false'  // Set to false to disable fill.
     };
// ===========================================

