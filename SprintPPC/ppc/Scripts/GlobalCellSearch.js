function atcGlobalCellSearch( className, searchBtnId, cellFldId, gridviewId, isSearchOn ) {

	this.className      = className;
	this.searchBtnId    = searchBtnId;
	this.cellFldId      = cellFldId;
	this.gridview       = document.getElementById( gridviewId );
	this.isSearchOn     = isSearchOn.toLowerCase();

    if( this.gridview != null ) {
        this.cellColIndex = this.getCellIndex();
        this.attachDblClick();
    }

    this.setFieldProps();

}

atcGlobalCellSearch.prototype.getCellIndex = function () {
    var headers = this.gridview.getElementsByTagName( 'th' );
    for ( var i = 0; i < headers.length; i++ ) {
        if ( headers[i].innerText.toLowerCase() === 'cell' ) {
            return i;
        }
    }
}

atcGlobalCellSearch.prototype.setFieldProps = function () {

    // getElemsByClass is defined in Site.js.
    var searchFields = getElemsByClass( this.className )
        , span
        , self = this
        , inputFld
		, img;

    for ( var i = 0; i < searchFields.length; i++ ) {

        // Remember the span element for use in the inner scope.
        span = searchFields[i];

        inputFld = span.getElementsByTagName( 'input' )[0];
        img = span.getElementsByTagName( 'img' )[0];

        // Set span properties
        inputFld.onfocusin = function () {
            span.style.border = '1pt solid #99B8CF';
        }
        inputFld.onfocusout = function () {
            span.style.border = '1pt solid #ccc';
        }
        inputFld.onmouseenter = function () {
            span.style.border = '1pt solid #99B8CF';
        }
        inputFld.onmouseleave = function () {
            if( document.activeElement != this ) {
                span.style.border = '1pt solid #ccc';
            }
        }

        // Set image properties
        img.style.display = this.isSearchOn === 'true' ? 'inline' : 'none';
        img.onclick = function () {
            inputFld.value = '';
            this.style.display = 'none';
            self.reloadGridView();
        }

    }

}

atcGlobalCellSearch.prototype.attachDblClick = function () {

    var rows = this.gridview.getElementsByTagName( 'tr' )
        , self = this    // Keep a reference to this object for use in the inner scope.
        , cellNum;

    for ( var i = 0; i < rows.length; i++ ) {

        rows[i].ondblclick = function () {
            cellNum = this.getElementsByTagName( 'td' )[self.cellColIndex].innerText;
            document.getElementById( self.cellFldId ).innerText = cellNum;
            self.reloadGridView();
        }

    }

}

atcGlobalCellSearch.prototype.reloadGridView = function () {
    __doPostBack( this.searchBtnId, '' );
}