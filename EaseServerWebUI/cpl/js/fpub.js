//get html paras
function parseQuery ( query ) {
           var Params = new Object ();
           if ( ! query ) return Params; 
           var Pairs = query.split(/[;&]/);
           for ( var i = 0; i < Pairs.length; i++ ) {
              var KeyVal = Pairs[i].split('=');
              if ( ! KeyVal || KeyVal.length != 2 ) continue;
              var key = unescape( KeyVal[0] );
              var val = decodeURIComponent( KeyVal[1] );
              val = val.replace(/\+/g, ' ');
              Params[key] = val;
           }
           return Params;
//var qs = location.search.replace("?","");
//var action = parseQuery(qs)["action"];
        }
//get time second 
function getTimeSec(){
     d = new Date();
     s=d.getSeconds()+'';
     s+= d.getUTCMilliseconds()+'';
   return s;
}
//
function fnAddSelOptionBlank_Sys(objSel,strValue,strText) {
		var oOption = document.createElement("OPTION");
		oOption.value = strValue;
		oOption.text = strText;
		objSel.options.add(oOption,0);
		objSel.value = strValue;
}
