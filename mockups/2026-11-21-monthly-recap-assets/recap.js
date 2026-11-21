const R={
 month:'novembre',Month:'Novembre',year:2026,days:30,firstWeekday:6,
 total:'1.284,60',count:96,
 prev:{name:'ottobre',total:'1.396,80',diff:'112,20',pct:'8%',lower:true},
 lastYear:{name:'novembre 2025',total:'1.236,10',diff:'48,50',pct:'4%',lower:false},
 daily:[12.4,0,38.2,8.6,54.1,22.3,71.8,46.5,0,14.2,29.9,9.8,63.4,186.4,41.2,6.5,33.7,18.9,52.6,88.3,74.1,27.5,0,11.3,36.8,24.6,58.9,92.7,39.4,0],
 record:{day:14,label:'Sabato 14 novembre',short:'sabato 14',total:'186,40',items:[
  {c:'mint',g:'shopping_cart',name:'Spesa',note:'Spesa della settimana',amt:'92,10'},
  {c:'butter',g:'restaurant',name:'Ristoranti',note:'Cena fuori',amt:'64,00'},
  {c:'sky',g:'local_gas_station',name:'Trasporti',note:'Benzina',amt:'30,30'}]},
 months:{labels:['G','F','M','A','M','G','L','A','S','O','N'],
  y2026:[1512,1198,1420,1355,1476,1390,1702,1948,1388,1397,1285],
  y2025:[1480,1302,1395,1410,1520,1455,1810,2010,1402,1400,1236]},
 rank:{text:'Il secondo mese più leggero del 2026',detail:'Solo febbraio è costato meno: 1.198 €.'},
 ytd:{total:'16.071',prevTotal:'16.420',diff:'349',pct:'2%',lower:true},
 cats:[
  {c:'peri',g:'home',name:'Casa',amt:'320,00',v:320,share:'25%'},
  {c:'mint',g:'shopping_cart',name:'Spesa',amt:'298,40',v:298.4,share:'23%'},
  {c:'butter',g:'restaurant',name:'Ristoranti',amt:'182,50',v:182.5,share:'14%'},
  {c:'sky',g:'directions_bus',name:'Trasporti',amt:'141,20',v:141.2,share:'11%'},
  {c:'lilac',g:'sports_esports',name:'Svago',amt:'96,30',v:96.3,share:'7%'},
  {c:'coral',g:'local_cafe',name:'Bar',amt:'58,20',v:58.2,share:'5%'},
  {c:'grey',g:'more_horiz',name:'Altre 3 categorie',amt:'188,00',v:188,share:'15%'}],
 up:{c:'butter',g:'restaurant',name:'Ristoranti',diff:'64,20',from:'118,30',to:'182,50'},
 down:{c:'sky',g:'directions_bus',name:'Trasporti',diff:'48,60',from:'189,80',to:'141,20'},
 habit:{c:'lilac',g:'pool',cat:'Svago',tag:'Piscina',times:5,amt:'42,50'},
 small:{count:31,total:'84,60',under:'5 €',top:{c:'coral',g:'local_cafe',tag:'Caffè',times:22,avg:'1,30'},
  mix:[['coral',22],['mint',5],['sky',4]]},
 streak:{days:26,of:30,empty:[2,9,23,30]}
};

const ms=(g,cls='',label='')=>`<span class="ms ${cls}"${label?` aria-label="${label}"`:' aria-hidden="true"'}>${g}</span>`;
const status=()=>`<div class="status"><span>9:41</span><span>${ms('signal_cellular_alt')} ${ms('wifi')} ${ms('battery_full')}</span></div>`;
const segs=(i,n)=>`<div class="segs" aria-hidden="true">${Array.from({length:n},(_,k)=>`<i class="${k<i?'done':k===i?'now':''}"></i>`).join('')}</div>`;
const storyTop=(i,n,extra='')=>`<div class="story-top">${segs(i,n)}<div class="bar-row"><b>Il tuo ${R.month}<small>${R.year}</small></b><span>${extra}<span class="ib">${ms('close','','Chiudi')}</span></span></div></div>`;
const cat=(c,g,size='')=>`<span class="tile ${size}" style="background:var(--${c})">${ms(g)}</span>`;

function strip(w,h,{hi=null,ink='var(--fg)',bar='var(--bar)',gap=3,emptyStub=true}={}){
 const n=R.daily.length,max=Math.max(...R.daily),bw=(w-gap*(n-1))/n;
 let s='';
 R.daily.forEach((v,k)=>{
  const x=Math.round(k*(bw+gap)*10)/10;
  if(!v){if(emptyStub)s+=`<rect x="${x}" y="${h-2}" width="${bw}" height="2" fill="var(--line)"/>`;return}
  const bh=Math.max(2,Math.round(v/max*h));
  s+=`<rect x="${x}" y="${h-bh}" width="${bw}" height="${bh}" fill="${k+1===hi?ink:bar}"/>`;
 });
 return `<svg class="chart" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}" shape-rendering="crispEdges" role="img" aria-label="Spesa giorno per giorno a ${R.month}: picco di ${R.record.total} € ${R.record.short}">${s}</svg>`;
}

function monthBars(w,h,{hi=10,label=true}={}){
 const v=R.months.y2026,n=v.length,gap=3,max=Math.max(...v),bw=(w-gap*(n-1))/n,top=18;
 let s='';
 v.forEach((m,k)=>{
  const x=k*(bw+gap),bh=Math.round(m/max*(h-top));
  const fill=k===hi?'var(--fg)':'var(--bar)';
  s+=`<rect x="${x}" y="${h-bh}" width="${bw}" height="${bh}" fill="${fill}"/>`;
  if(label)s+=`<text x="${x+bw/2}" y="${h+16}" text-anchor="middle" font-size="12" font-weight="${k===hi?700:400}" fill="${k===hi?'var(--fg)':'var(--mute)'}">${R.months.labels[k]}</text>`;
 });
 const f=v[1],fx=bw+gap,fh=Math.round(f/max*(h-top));
 s+=`<text x="${fx+bw/2}" y="${h-fh-6}" text-anchor="middle" font-size="12" font-weight="600" fill="var(--mute)">1.198</text>`;
 const nx=10*(bw+gap),nh=Math.round(v[10]/max*(h-top));
 s+=`<text x="${nx+bw/2}" y="${h-nh-6}" text-anchor="middle" font-size="12" font-weight="700" fill="var(--fg)">1.285</text>`;
 return `<svg class="chart" width="${w}" height="${h+22}" viewBox="0 0 ${w} ${h+22}" shape-rendering="crispEdges" role="img" aria-label="Spesa per mese nel 2026: novembre 1.285 €, il più leggero dopo febbraio">${s}</svg>`;
}

function cumulative(w,h){
 const a=R.months.y2026,b=R.months.y2025,n=12,gap=2,bw=(w-gap*(n-1))/n;
 const ca=[],cb=[];a.reduce((s,v,k)=>(ca[k]=s+v),0);b.reduce((s,v,k)=>(cb[k]=s+v),0);
 const max=21000;let s='';
 for(let k=0;k<n;k++){
  const x=k*(bw+gap);
  if(k<a.length){const bh=Math.round(ca[k]/max*h);s+=`<rect x="${x}" y="${h-bh}" width="${bw}" height="${bh}" fill="${k===a.length-1?'var(--fg)':'var(--bar)'}"/>`}
  else s+=`<rect x="${x}" y="${h-4}" width="${bw}" height="4" fill="var(--line)"/>`;
 }
 let d='';cb.forEach((v,k)=>{const x=k*(bw+gap),y=Math.round(h-v/max*h);d+=`${k?'L':'M'}${x} ${y} L${x+bw+(k<cb.length-1?gap:0)} ${y} `});
 s+=`<path d="${d}" fill="none" stroke="var(--fg)" stroke-width="2" stroke-dasharray="4 3"/>`;
 return `<svg class="chart" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}" shape-rendering="crispEdges" role="img" aria-label="Spesa accumulata nel 2026 contro il 2025: 16.071 € contro 16.420 € a fine novembre">${s}</svg>`;
}

function calendar(cell,gap,{on='var(--fg)',off='var(--line)'}={}){
 const lead=R.firstWeekday;let s='<div class="cal" role="img" aria-label="Spese registrate in 26 giorni su 30" style="display:grid;grid-template-columns:repeat(7,'+cell+'px);gap:'+gap+'px">';
 ['L','M','M','G','V','S','D'].forEach(d=>s+=`<span style="font-size:12px;color:var(--mute);text-align:center">${d}</span>`);
 for(let k=0;k<lead;k++)s+='<span></span>';
 for(let d=1;d<=R.days;d++){const empty=R.streak.empty.includes(d);s+=`<span style="height:${cell}px;${empty?`outline:1px solid ${off};outline-offset:-1px`:`background:${on}`}"></span>`}
 return s+'</div>';
}

function smallUnits(cell,gap,cols){
 let s=`<div role="img" aria-label="31 spese sotto i 5 €: 22 al bar, 5 di spesa, 4 di trasporti" style="display:grid;grid-template-columns:repeat(${cols},${cell}px);gap:${gap}px">`;
 R.small.mix.forEach(([c,n])=>{for(let k=0;k<n;k++)s+=`<span style="height:${cell}px;background:var(--${c})"></span>`});
 return s+'</div>';
}

function wireTheme(){
 document.querySelectorAll('.theme button').forEach(b=>b.addEventListener('click',()=>{
  document.documentElement.classList.toggle('dark',b.dataset.theme==='dark');
  document.querySelectorAll('.theme button').forEach(x=>x.setAttribute('aria-pressed',x===b));
 }));
}
