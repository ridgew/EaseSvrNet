/// <reference path="../jquery.js" />
$(function() {
    var roleId = $.QueryString().RoleId;
    var roleName = $.QueryString().RoleName;
    var roleDescription = $.QueryString().RoleDescription;
    var tit = roleDescription + '[' + roleName + ']';
    $('#roleInfo').html(tit);
    
    var _rps = [];

    var strTitle = '资源类型';
    var jQueryUL = $('#tree');
    var strModeCode = '1.3.2.3.4';
    var strPostData = '{}';
    var jsonConfig = { 'ID': 'ResTypeID', 'Name': 'ResTypeName', 'hasChild': 'ResTypeClassNum' };

    var imgObj = function() {
        var oipt = $('<input type="checkbox" value="'+ this.ResTypeID +'" name="restype" id="restype'+ this.ResTypeID +'" '+ checkExist(this.ResTypeID,0) +'/>').click(function(){
            if(this.checked){
                $(this.parentNode.parentNode).find('input').each(function(){this.checked=true});
            } else {
                $(this.parentNode.parentNode).find('input').each(function(){this.checked=false});
            }        
        });
        return $('<div></div>').append(oipt).append('<label for="restype'+ this.ResTypeID +'"> ' + this.ResTypeName + '</label>');
    };

    var fnGetTree = function() {
        var strTitle = '资源类型的细分类别';
        var strModeCode = '1.3.2.5.4';
        var strPostData = '{"ResTypeID":"' + this.value + '","ResClassParentID":0}';
        if ($(this).attr("protocol") != '1.3.2.3.4')
            strPostData = '{"ResClassParentID":"' + this.value + '","ResTypeID":0}';
        var jsonConfig = { 'ID': 'ResClassID', 'Name': 'ResClassName', 'hasChild': 'ResChildRenNum' };

        var imgObjClass = function() {
            var oipt = $('<input type="checkbox" value="'+ this.ResClassID +'" name="resclass" id="resclass'+ this.ResClassID +'" '+ checkExist(this.ResClassID,1) +'/>').click(function(){
                if(this.checked){
                    $(this.parentNode.parentNode).find('input').each(function(){this.checked=true});
                    var fnCheckParent = function(obj){
                        var pIpt = $(obj.parentNode.parentNode.parentNode.parentNode).find('>div>input');
                        if(pIpt[0]&&(!pIpt[0].checked)){
                            pIpt[0].checked=true;
                            fnCheckParent(pIpt[0]);
                        }
                    }
                    fnCheckParent(this);                    
                } else {
                    $(this.parentNode.parentNode).find('input').each(function(){this.checked=false});
                }        
            });
            return $('<div></div>').append(oipt).append('<label for="resclass'+ this.ResClassID +'">' + this.ResClassName + '</label>');
        };

        getTree(strTitle, $(this).find('>ul'), strModeCode, strPostData, jsonConfig, null, fnGetTree, null, imgObjClass)
    };
        
    var ajaxConfig = {url:'./service/1.3.5.2.12',param:{roleid:roleId}};
    var ajaxObj = new top.AJAXFunction(ajaxConfig);
    ajaxObj.SuFun = function(responseObj){
        _rps = responseObj.d.Data;        
        getTree(strTitle, jQueryUL, strModeCode, strPostData, jsonConfig, null, fnGetTree, null, imgObj);        
    }
    ajaxObj.SendFun();
    
    function checkExist(id,type){
        for(var i=0; i<_rps.length; i++){
            if(_rps[i].id==id && _rps[i].type==type)return ' checked="checked"';
        }
        return '';
    }
    
    
    
    $('#btnsave').click(function(){
        var oldrtids='',oldrcids='',restypeid='',resclassid='';
        for(var i=0; i<_rps.length; i++)
        {
            if(_rps[i].type==0) oldrtids=oldrtids==''?_rps[i].id:oldrtids+','+_rps[i].id;
            else oldrcids=oldrcids==''?_rps[i].id:oldrcids+','+_rps[i].id;
        }
        
        $('input[name="restype"]').each(function(){
			var re = new RegExp('(^'+this.value+',)|(,'+this.value+',)|(,'+this.value+'$)|(^'+this.value+'$)','ig');			
			if (re.test(oldrtids)){
				if (!this.checked) {
					var match = RegExp.lastMatch;
					if (match.indexOf(',')==match.lastIndexOf(','))	{
						oldrtids = oldrtids.replace(match,'');
					} else {
						oldrtids = oldrtids.replace(match,',');
					}				
				}				
			} else {
				if (this.checked) {
					restypeid = restypeid==''?this.value:restypeid+','+this.value;
				}				
			}
        });
		restypeid = restypeid==''?oldrtids:restypeid+(oldrtids==''?'':',')+oldrtids;

        $('input[name="resclass"]').each(function(){
			var re = new RegExp('(^'+this.value+',)|(,'+this.value+',)|(,'+this.value+'$)|(^'+this.value+'$)','ig');
			if (re.test(oldrcids)){
				if (!this.checked) {
					var match = RegExp.lastMatch;
					if (match.indexOf(',')==match.lastIndexOf(','))	{
						oldrcids = oldrcids.replace(match,'');
					} else {
						oldrcids = oldrcids.replace(match,',');
					}				
				}				
			} else {
				if (this.checked) {
					resclassid = resclassid==''?this.value:resclassid+','+this.value;
				}
			}         
        });
		resclassid = resclassid==''?oldrcids:resclassid+(oldrcids==''?'':',')+oldrcids;
        
        var ajaxConfig = {url:'./service/1.3.5.2.11',param:{roleid:roleId,restids:restypeid,rescids:resclassid}};
        var ajaxObj = new top.AJAXFunction(ajaxConfig);
        ajaxObj.SuFun = function(responseObj){
            alert('设置资源管理权限成功');
        }
        ajaxObj.SendFun();
    });
});
