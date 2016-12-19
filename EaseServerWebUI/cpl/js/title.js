//***********默认设置定义.*********************
tPopWait=5;//停留tWait豪秒后显示提示。
tPopShow=6000000;//显示tShow豪秒后关闭提示
showPopStep=20;
popOpacity=99;

//***************内部变量定义*****************
sPop=null;
curShow=null;
tFadeOut=null;
tFadeIn=null;
tFadeWaiting=null;

if(document.all){
    document.onmouseover = showPopupText;
    window.onload = function(){
        var tsTempDiv = document.createElement('DIV');
        tsTempDiv.innerHTML = '<div id="dypopLayer" style="position:absolute;z-index:1000;background-color: #F8F8F5;color:#000000; border: 1px #000000 solid;font-color: font-size: 12px; padding-right: 4px; padding-left: 4px; padding-top: 2px; padding-bottom: 2px; filter: Alpha(Opacity=0)"></div>';
        document.body.appendChild(tsTempDiv);
    }
}

function showPopupText(){
    if(!document.getElementById('dypopLayer'))return;
    var evt = window.event||arguments[0];
    var o=evt.srcElement||evt.target;
	MouseX=evt.x;
	MouseY=evt.y;
	if(o.alt!=null && o.alt!=""){o.dypop=o.alt;o.alt=""};
        if(o.title!=null && o.title!=""){o.dypop=o.title;o.title=""};
	if(o.dypop!=sPop) {
			sPop=o.dypop;
			clearTimeout(curShow);
			clearTimeout(tFadeOut);
			clearTimeout(tFadeIn);
			clearTimeout(tFadeWaiting);	
			if(sPop==null || sPop=="") {
				document.getElementById('dypopLayer').innerHTML="";
				document.getElementById('dypopLayer').style.filter="Alpha()";
				document.getElementById('dypopLayer').filters.Alpha.opacity=0;	
				}
			else {
				if(o.dyclass!=null) popStyle=o.dyclass 
					else popStyle="cPopText";
				curShow=setTimeout("showIt()",tPopWait);
			}
			
	}
}

function showIt(){
		document.getElementById('dypopLayer').className=popStyle;
		document.getElementById('dypopLayer').innerHTML=sPop;
		popWidth=document.getElementById('dypopLayer').clientWidth;
		popHeight=document.getElementById('dypopLayer').clientHeight;
		if(MouseX+12+popWidth>document.body.clientWidth) popLeftAdjust=-popWidth-24
			else popLeftAdjust=0;
		if(MouseY+12+popHeight>document.body.clientHeight) popTopAdjust=-popHeight-24
			else popTopAdjust=0;
		document.getElementById('dypopLayer').style.left=MouseX+12+document.body.scrollLeft+popLeftAdjust;
		document.getElementById('dypopLayer').style.top=MouseY+12+document.body.scrollTop+popTopAdjust;
		document.getElementById('dypopLayer').style.filter="Alpha(Opacity=0)";
		fadeOut();
}

function fadeOut(){
	if(document.getElementById('dypopLayer').filters.Alpha.opacity<popOpacity) {
		document.getElementById('dypopLayer').filters.Alpha.opacity+=showPopStep;
		tFadeOut=setTimeout("fadeOut()",1);
		}
		else {
			document.getElementById('dypopLayer').filters.Alpha.opacity=popOpacity;
			tFadeWaiting=setTimeout("fadeIn()",tPopShow);
		}
}

function fadeIn(){
	if(document.getElementById('dypopLayer').filters.Alpha.opacity>0) {
		document.getElementById('dypopLayer').filters.Alpha.opacity-=1;
		tFadeIn=setTimeout("fadeIn()",1);
	}
}
