$(function(){
 var roleId = $.QueryString().RoleId;
    var roleName = $.QueryString().RoleName;
    var roleDescription = $.QueryString().RoleDescription;
    var tit = roleDescription + '[' + roleName + ']';
    $('#roleInfo').html(tit);
    
    var _rps = [];
    
    var strTitle = '菜单管理';
    var jQueryUL = $('#tree');
    var strModeCode = '1.3.5.1.3';
    var strPostData = '{parentId:0}';
    var jsonConfig = { 'ID': 'MenuId', 'Name': 'MenuName', 'hasChild': 'HasChildren' };

    var MenuItem = function() {
        var oipt = $('<input type="checkbox" value="'+ this.MenuId +'" name="menu" id="menu'+ this.MenuId +'" '+ checkExist(this.MenuId,0) +'/>').click(function(){
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
        return $('<div></div>').append(oipt).append('<label for="menu'+ this.MenuId +'"> ' + this.MenuName + '</label>');
    };

    var fnGetTree = function() {
        var strTitle = '菜单管理';
        var strModeCode = '1.3.5.1.3';
        var strPostData = '{parentId:' + this.value + '}';
        var jsonConfig = { 'ID': 'MenuId', 'Name': 'MenuName', 'hasChild': 'HasChildren' };
        
        getTree(strTitle, $(this).find('>ul'), strModeCode, strPostData, jsonConfig, null, fnGetTree, null, MenuItem);
    };

    var ajaxConfig = {url:'./service/1.3.5.2.15',param:{roleId:roleId}};
    var ajaxObj = new top.AJAXFunction(ajaxConfig);
        ajaxObj.SuFun = function(responseObj){
            _rps = responseObj.d.Data;        
        getTree(strTitle, jQueryUL, strModeCode, strPostData, jsonConfig, null, fnGetTree, null, MenuItem);
    }
    ajaxObj.SendFun();
    
    function checkExist(id,type){
        for(var i=0; i<_rps.length; i++){
            if(_rps[i].id==id && _rps[i].type==type)return ' checked="checked"';
        }
        return '';
    }
    
    $('#btnsave').click(function(){
        var oldmenuids='',menuid='';
        for(var i=0; i<_rps.length; i++)
        {
            oldmenuids=oldmenuids==''?_rps[i].id:oldmenuids+','+_rps[i].id;
        }
        
        $('input[name="menu"]').each(function(){
			var re = new RegExp('(^'+this.value+',)|(,'+this.value+',)|(,'+this.value+'$)|(^'+this.value+'$)','ig');			
			if (re.test(oldmenuids)){
				if (!this.checked) {
					var match = RegExp.lastMatch;
					if (match.indexOf(',')==match.lastIndexOf(','))	{
						oldmenuids = oldmenuids.replace(match,'');
					} else {
						oldmenuids = oldmenuids.replace(match,',');
					}				
				}				
			} else {
				if (this.checked) {
					menuid = menuid==''?this.value:menuid+','+this.value;
				}				
			}
        });
		menuid = menuid==''?oldmenuids:menuid+(oldmenuids==''?'':',')+oldmenuids;
        
        var ajaxConfig = {url:'./service/1.3.5.2.16',param:{roleid:roleId,menuids:menuid}};
        var ajaxObj = new top.AJAXFunction(ajaxConfig);
        ajaxObj.SuFun = function(responseObj){
            alert('设置菜单管理权限成功');
        }
        ajaxObj.SendFun();
    });
});