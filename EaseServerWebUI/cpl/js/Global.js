//http://refresh-sf.com/yui/
String.prototype.toggle=function(value,other){return this==value?other:value};Object.defined=function(obj,prop){return typeof(obj)!="undefined"&&(prop==null||obj.hasOwnProperty(prop))};String.prototype.substitute=function(o,fn){var s=this,preserveEscaped=true;function txtReplace(obj,str,f){var patternExpr=/\{([^\{\}]*)\}/g;return str.replace(patternExpr,function($str,rltxt){if($str=="{}"&&preserveEscaped){return $str}if(!rltxt){return""}try{var r=eval("with(obj){"+$str+"}"),m=obj.decorator;if(m!="undefined"&&typeof(m)=="function"){f=m}if(Object.defined(r)&&typeof(r)!="object"){return Object.defined(r)?(f?f(r):r):(preserveEscaped?r:"")}else{return $str}}catch(ex){return $str}})}if(o.constructor!=Array){o=[o]}for(var i=0,j=o.length;i<j;++i){if(i==j-1){preserveEscaped=false}s=txtReplace(o[i],s,fn)}return s};var request={QueryString:function(val){var uri="?"+window.location.search;var re=new RegExp("[?|&]"+val+"=([^&?]*)","ig");return((uri.match(re))?(uri.match(re)[0].substr(val.length+2)):null)},QueryStrings:function(){var uri=window.location.search;var re=/\w*\=([^\&\?]*)/ig;var retval=[];while((arr=re.exec(uri))!=null){retval.push(arr[0])}return retval},setQuery:function(val1,val2){var a=this.QueryStrings();var retval="";var seted=false;var re=new RegExp("^"+val1+"=([^&?]*)$","ig");for(var i=0;i<a.length;i++){if(re.test(a[i])){seted=true;a[i]=val1+"="+val2}}retval=a.join("&");return"?"+retval+(seted?"":(retval?"&":"")+val1+"="+val2)}};function hashTable(){this.name=(arguments[0]!=null)?arguments[0]:null;this._hash=new Object();this.add=function(key,value){if(typeof(key)!="undefined"){if(this.contains(key)==false){this._hash[key]=typeof(value)=="undefined"?null:value;return true}else{return false}}else{return false}};this.remove=function(key){delete this._hash[key]};this.count=function(){var i=0;for(var k in this._hash){i++}return i};this.items=function(key){return this._hash[key]};this.contains=function(key){return typeof(this._hash[key])!="undefined"};this.clear=function(){for(var k in this._hash){delete this._hash[k]}}}var WIMP={namespace:function(){var a=arguments,o=null,i,j,d,rt;for(i=0;i<a.length;++i){d=a[i].split(".");rt=d[0];eval("if (typeof "+rt+' == "undefined"){'+rt+" = {};} o = "+rt+";");for(j=1;j<d.length;++j){o[d[j]]=o[d[j]]||{};o=o[d[j]]}}},override:function(origclass,overrides){if(overrides){var p=origclass.prototype;for(var method in overrides){p[method]=overrides[method]}}}};WIMP.ns=WIMP.namespace;WIMP.ns("WIMP.DesktopParts","WIMP.UIHelper");WIMP.apply=function(o,c,defaults){if(defaults){WIMP.apply(o,defaults)}if(o&&c&&typeof c=="object"){for(var p in c){o[p]=c[p]}}return o};WIMP.applyIf=function(o,c){if(o&&c){for(var p in c){if(typeof o[p]=="undefined"){o[p]=c[p]}}}return o};WIMP.apply(Function.prototype,{createCallback:function(){var args=arguments;var method=this;return function(){return method.apply(window,args)}},createDelegate:function(obj,args,appendArgs){var method=this;return function(){var callArgs=args||arguments;if(appendArgs===true){callArgs=Array.prototype.slice.call(arguments,0);callArgs=callArgs.concat(args)}else{if(typeof appendArgs=="number"){callArgs=Array.prototype.slice.call(arguments,0);var applyArgs=[appendArgs,0].concat(args);Array.prototype.splice.apply(callArgs,applyArgs)}}return method.apply(obj||window,callArgs)}},defer:function(millis,obj,args,appendArgs){var fn=this.createDelegate(obj,args,appendArgs);if(millis){return setTimeout(fn,millis)}fn();return 0},createSequence:function(fcn,scope){if(typeof fcn!="function"){return this}var method=this;return function(){var retval=method.apply(this||window,arguments);fcn.apply(scope||this||window,arguments);return retval}},createInterceptor:function(fcn,scope){if(typeof fcn!="function"){return this}var method=this;return function(){fcn.target=this;fcn.method=method;if(fcn.apply(scope||this||window,arguments)===false){return}return method.apply(this||window,arguments)}}});WIMP.applyIf(String,{escape:function(string){return string.replace(/('|\\)/g,"\\$1")},format:function(format){var args=Array.prototype.slice.call(arguments,1);return format.replace(/\{(\d+)\}/g,function(m,i){return args[i]})}});

