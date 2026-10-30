// Original geometric moth preview; contains no extracted game art.
import {writeFileSync,mkdirSync} from 'node:fs';
import {deflateSync} from 'node:zlib';
import {fileURLToPath} from 'node:url';
import {createProject} from '../src/model-v2.js';
const width=64,height=64,pixels=Buffer.alloc((width*4+1)*height);
for(let y=0;y<height;y++)for(let x=0;x<width;x++){
 const dx=x-31.5,dy=y-31.5;let c=null;
 if(Math.abs(dx)>3&&Math.abs(dx)<26&&(dx*dx/650+dy*dy/450)<1)c=[51,144,151,255];
 if(Math.abs(dx)>6&&Math.abs(dx)<21&&(dx*dx/520+dy*dy/300)<1)c=[110,216,197,255];
 if(Math.abs(dx)<4&&Math.abs(dy)<20)c=[31,64,71,255];
 if(Math.abs(dx)<2&&Math.abs(dy)<15)c=[244,206,112,255];
 if((Math.abs(dx-12)<3||Math.abs(dx+12)<3)&&Math.abs(dy+3)<3)c=[255,239,181,255];
 if(c)for(let k=0;k<4;k++)pixels[y*(width*4+1)+1+x*4+k]=c[k];
}
function crc(buf){let c=0xffffffff;for(const b of buf){c^=b;for(let k=0;k<8;k++)c=(c>>>1)^((c&1)?0xedb88320:0);}return(c^0xffffffff)>>>0;}
function chunk(name,data){const t=Buffer.from(name),n=Buffer.alloc(4),check=Buffer.alloc(4);n.writeUInt32BE(data.length);check.writeUInt32BE(crc(Buffer.concat([t,data])));return Buffer.concat([n,t,data,check]);}
const header=Buffer.alloc(13);header.writeUInt32BE(width);header.writeUInt32BE(height,4);header[8]=8;header[9]=6;
const png=Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',header),chunk('IDAT',deflateSync(pixels)),chunk('IEND',Buffer.alloc(0))]);
const out=fileURLToPath(new URL('../../../author-sdk/examples/monster-compendium/',import.meta.url));mkdirSync(out,{recursive:true});writeFileSync(out+'preview.png',png);
const p=createProject();p.pack={id:'Samples.MonsterCompendium',name:'青灯蛾图鉴',author:'DTMAPI',version:'0.1.0'};
p.assets=[{id:'moth',name:'原创青灯蛾预览',dataUrl:'data:image/png;base64,'+png.toString('base64'),width,height}];
const m=p.monsters[0];m.id='lantern';m.templateId='bee';m.overrides={health:40,display:{name:'青灯蛾·配置',nameplate:true,healthBar:'Always'}};
m.compendium={title:'青灯蛾·配置',habitat:'旧温室的阴影里',description:'这只蛾的翅膀像两片浅绿色叶子。\n示例条目在首次击败后开放，击杀次数由游戏记录。',category:'Organism',showLocked:true,assetId:'moth'};
writeFileSync(new URL('../samples/compendium.dtmonster.json',import.meta.url),JSON.stringify(p,null,2)+'\n');
