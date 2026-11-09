const http=require('node:http');
const zlib=require('node:zlib');
let uploadObserved=false;
const server=http.createServer(async(req,res)=>{
  if(new URL(req.url,'http://localhost').pathname==='/upload-progress')req.on('data',()=>{uploadObserved=true;});
  const chunks=[];
  try{for await(const chunk of req)chunks.push(chunk);}catch{return;}
  const body=Buffer.concat(chunks).toString('utf8');
  const path=new URL(req.url,'http://localhost').pathname;
  const text=(status,value,headers={})=>{res.writeHead(status,{'content-type':'text/plain;charset=utf-8',...headers});res.end(value);};
  if(path==='/upload-reset'){uploadObserved=false;return text(200,'reset');}
  if(path==='/upload-observed')return text(200,String(uploadObserved));
  if(path==='/upload-progress')return text(200,body);
  if(path==='/text')return text(200,'hello',{'x-test':'value'});
  if(path==='/echo'){res.writeHead(200,{'content-type':'application/json'});return res.end(JSON.stringify({method:req.method,body,header:req.headers['x-test']??null,contentType:req.headers['content-type']??null}));}
  if(path==='/redirect'){res.writeHead(302,{location:'/text'});return res.end();}
  if(path==='/redirect303'){res.writeHead(303,{location:'/echo'});return res.end();}
  if(path==='/redirect307'){res.writeHead(307,{location:'/echo'});return res.end();}
  if(path==='/loop'){res.writeHead(302,{location:'/loop'});return res.end();}
  if(path==='/empty'){res.writeHead(204);return res.end();}
  if(path==='/slow'){setTimeout(()=>text(200,'slow'),100);return;}
  if(path==='/gzip'){res.writeHead(200,{'content-type':'text/plain','content-encoding':'gzip'});return res.end(zlib.gzipSync('compressed'));}
  if(path==='/deflate'){res.writeHead(200,{'content-type':'text/plain','content-encoding':'deflate'});return res.end(zlib.deflateSync('compressed'));}
  if(path==='/brotli'){res.writeHead(200,{'content-type':'text/plain','content-encoding':'br'});return res.end(zlib.brotliCompressSync('compressed'));}
  text(404,'missing');
});
server.listen(0,'127.0.0.1',()=>console.log(server.address().port));
