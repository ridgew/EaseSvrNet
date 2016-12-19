/// <reference path="../jquery.js" />
var _rps = [];
$(function() {
    var roleId = $.QueryString().RoleId;
    var roleName = $.QueryString().RoleName;
    var roleDescription = $.QueryString().RoleDescription;
    var tit = roleDescription + '[' + roleName + ']';
    $('#roleInfo').html(tit);
    //定义旧的权限数组
	//请求并获取已有的权限
	var ajaxConfig = {url:'./service/1.3.5.2.14',param:{roleid:roleId}};
    var ajaxObj = new top.AJAXFunction(ajaxConfig);
    ajaxObj.SuFun = function(responseObj){
        _rps = responseObj.d.Data;
		//请求业务列表
		GetProductClassTree();
    }
    ajaxObj.SendFun();
    
    
    $('#btnsave').click(function(){        
		var pcid='',pid='',pgid='',pgcid='',oldpcid='',oldpid='',oldpgid='',oldpgcid='';
        for(var i=0; i<_rps.length; i++)
        {
            if(_rps[i].type==0) oldpcid=oldpcid==''?_rps[i].id:oldpcid+','+_rps[i].id;
			else if(_rps[i].type==1) oldpid=oldpid==''?_rps[i].id:oldpid+','+_rps[i].id;
			else if(_rps[i].type==2) oldpgid=oldpgid==''?_rps[i].id:oldpgid+','+_rps[i].id;
            else oldpgcid=oldpgcid==''?_rps[i].id:oldpgcid+','+_rps[i].id;
        }

        $('input[name="pc"]').each(function(){
            var re = new RegExp('(^'+this.value+',)|(,'+this.value+',)|(,'+this.value+'$)|(^'+this.value+'$)','ig');			
			if (re.test(oldpcid)){
				if (!this.checked) {
					var match = RegExp.lastMatch;
					if (match.indexOf(',')==match.lastIndexOf(','))	{
						oldpcid = oldpcid.replace(match,'');
					} else {
						oldpcid = oldpcid.replace(match,',');
					}				
				}				
			} else {
				if (this.checked) {
					pcid = pcid==''?this.value:pcid+','+this.value;
				}				
			}
        });
		pcid = pcid==''?oldpcid:pcid+(oldpcid==''?'':',')+oldpcid;

        $('input[name="p"]').each(function(){
            var re = new RegExp('(^'+this.value+',)|(,'+this.value+',)|(,'+this.value+'$)|(^'+this.value+'$)','ig');			
			if (re.test(oldpid)){
				if (!this.checked) {
					var match = RegExp.lastMatch;
					if (match.indexOf(',')==match.lastIndexOf(','))	{
						oldpid = oldpid.replace(match,'');
					} else {
						oldpid = oldpid.replace(match,',');
					}				
				}				
			} else {
				if (this.checked) {
					pid = pid==''?this.value:pid+','+this.value;
				}				
			}
        });
		pid = pid==''?oldpid:pid+(oldpid==''?'':',')+oldpid;

        /*$('input[name="pg"]').each(function(){
            var re = new RegExp('(^'+this.value+',)|(,'+this.value+',)|(,'+this.value+'$)|(^'+this.value+'$)','ig');			
			if (re.test(oldpgid)){
				if (!this.checked) {
					var match = RegExp.lastMatch;
					if (match.indexOf(',')==match.lastIndexOf(','))	{
						oldpgid = oldpgid.replace(match,'');
					} else {
						oldpgid = oldpgid.replace(match,',');
					}				
				}				
			} else {
				if (this.checked) {
					pgid = pgid==''?this.value:pgid+','+this.value;
				}				
			}
        });
		pgid = pgid==''?oldpgid:pgid+(oldpgid==''?'':',')+oldpgid;*/

        $('input[name="pgc"]').each(function(){
            var re = new RegExp('(^'+this.value+',)|(,'+this.value+',)|(,'+this.value+'$)|(^'+this.value+'$)','ig');			
			if (re.test(oldpgcid)){
				if (!this.checked) {
					var match = RegExp.lastMatch;
					if (match.indexOf(',')==match.lastIndexOf(','))	{
						oldpgcid = oldpgcid.replace(match,'');
					} else {
						oldpgcid = oldpgcid.replace(match,',');
					}				
				}				
			} else {
				if (this.checked) {
					pgcid = pgcid==''?this.value:pgcid+','+this.value;
				}				
			}
        });
		pgcid = pgcid==''?oldpgcid:pgcid+(oldpgcid==''?'':',')+oldpgcid;
        
        var ajaxConfig = {url:'./service/1.3.5.2.13',param:{roleid:roleId,pcids:pcid,pids:pid,pgids:pgid,pgcids:pgcid}};
        var ajaxObj = new top.AJAXFunction(ajaxConfig);
        ajaxObj.SuFun = function(responseObj){
            alert('设置业务管理权限成功');
        }
        ajaxObj.SendFun();
    });
});




//获取一级业务数据分类数据树
function GetProductClassTree()
{
	var strTitle = '业务分类';
    var jQueryUL = $('#tree');
    var strModeCode = '1.3.3.1.0';
	var strRightCode = '3.3.1.0';
	var strPostData = '{"Right":false}';
    var jsonConfig = {'ID':'PClassID','Name':'PClassName','hasChild':'ProductNum'};
	var imgObj = function() {
	    var oipt = $('<input type="checkbox" value="'+ this.PClassID +'" name="pc" id="pc'+ this.PClassID +'" '+ checkExist(this.PClassID,0) +'/>').click(function(){
            if(this.checked){
                $(this.parentNode.parentNode).find('input').each(function(){this.checked=true});
            } else {
                $(this.parentNode.parentNode).find('input').each(function(){this.checked=false});
            }        
        });
        return $('<div></div>').append(oipt).append('<label for="pc'+ this.PClassID +'"> ' + this.PClassName + '</label>');
    };
	var fnGetTree = function(){GetProTreeFun(this.value,$(this).find('>ul'));};
	getTree(strTitle,jQueryUL,strModeCode,strPostData,jsonConfig,strRightCode,fnGetTree,null,imgObj);
}


function GetProTreeFun(Pcid,jQueryUL)
{
	var strTitle = '业务数据';
	var strModeCode = '1.3.3.1.4';
	var strPostData = '{"PClassID":"'+Pcid+'","Right":false}';
	var jsonConfig = {'ID':'ProductID','Name':'ProductName','hasChild':'PageNum'};
	var imgObjClass = function(){ 
	    var oipt = $('<input type="checkbox" value="'+ this.ProductID +'" name="p" id="p'+ this.ProductID +'" '+ checkExist(this.ProductID,1) +'/>').click(function(){
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
        return $('<div></div>').append(oipt).append('<label for="p'+ this.ProductID +'"> ' + this.ProductName + '</label>');
	};
	var fnGetTree = function(){GetProModuleTreeFun(this.value,$(this).find('>ul'),$(this).attr("protocol"));};
	getTree(strTitle,jQueryUL,strModeCode,strPostData,jsonConfig,'3.3.1.4',fnGetTree,null,imgObjClass);  
}


function GetProModuleTreeFun(Pid,jQueryUL,Pro)
{
	var strTitle = '业务栏目';
	var strModeCode = '1.3.3.1.8';
	var strPostData = '{"ProductID":0,"ParentID":' + Pid + ',"Right":false}';
	if (Pro == "1.3.3.1.4")
		strPostData = '{"ProductID":' + Pid + ',"ParentID":0,"Right":false}';
	var jsonConfig = {'ID':'ModuleID','Name':'ModuleName','hasChild':'ChildNum'};
	var imgObjClass = function() 
	{
		var _id = getModuleID(this.ModuleID.toString());
		var oipt = $('<input type="checkbox" value="'+ _id +'" name="pgc" id="pgc'+ _id +'" '+ checkExist(_id,2) +'/>').click(function(){
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
        return $('<div></div>').append(oipt).append('<label for="pgc'+ _id +'"> ' + this.ModuleName + '</label>');
	};
	var fnGetTree = function(){ GetProModuleTreeFun(getModuleID(this.value.toString()),$(this).find('>ul'),$(this).attr("protocol"));};
	getTree(strTitle,jQueryUL,strModeCode,strPostData,jsonConfig,'3.3.1.8',fnGetTree,null,imgObjClass);  
}


function getModuleID(mID)
{
	return mID.indexOf("_") >= 0 ? mID.split("_")[0] : mID
}


function checkExist(id,type){
	for(var i=0; i<_rps.length; i++){
		if(_rps[i].id==id && _rps[i].type==type)return ' checked="checked"';
	}
	return '';
}