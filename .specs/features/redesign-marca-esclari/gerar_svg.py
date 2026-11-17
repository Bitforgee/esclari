import xml.etree.ElementTree as ET
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[3]
DESTINO = Path(__file__).resolve().parent / "conceitos"
DESTINO.mkdir(parents=True, exist_ok=True)
MARCA_ATUAL = RAIZ / "web/public/marca/logo-horizontal.svg"
raiz = ET.fromstring(MARCA_ATUAL.read_text(encoding="utf-8"))
paths = [e for e in raiz if e.tag.endswith("path")]
PALAVRA = paths[1].attrib["d"]
SCLARI = PALAVRA[PALAVRA.index("M108.05"):]

FONTE_V1 = "M128 28C72 28 32 70 32 128C32 186 74 228 136 228C177 228 208 210 228 184L194 160C181 177 162 188 138 188C109 188 89 174 81 150H228V126C228 69 190 28 128 28ZM84 112C91 84 108 70 134 70C159 70 177 85 182 112H84ZM184 28H210V54H198V66L184 54Z"
FONTE_V2 = "M128 24C69 24 28 68 28 128C28 188 72 232 136 232C179 232 212 212 234 182L196 156C183 176 163 188 138 188C108 188 88 174 80 150H232V126C232 67 192 24 128 24ZM82 112C90 82 107 68 134 68C161 68 179 84 184 112H82ZM170 24H210V62H194V80L170 62Z"

MANICULA_V1 = "M44 86C44 72 56 60 70 60H112V46C112 28 126 14 144 14H224C238 14 246 24 246 38V68C246 82 236 92 222 92H162L216 126C229 134 232 150 224 162C216 174 199 176 188 167L162 148V196C162 222 142 242 116 242H78C50 242 30 220 30 192V102C30 94 36 88 44 86Z"
MANICULA_V2 = "M24 96H76L122 50C132 38 148 36 160 46C174 56 176 74 166 88L154 102H222C238 102 246 114 246 128C246 142 238 154 222 154H188C178 188 148 212 110 212H72V228H24ZM76 116H136V132H76ZM76 148H124V164H76Z"

FIO_V1 = "M36 42H150C190 42 220 72 220 110C220 148 190 178 150 178H102C91 178 84 185 84 196C84 207 91 214 102 214H230V238H102C76 238 58 220 58 196C58 170 76 152 102 152H150C174 152 192 134 192 110C192 86 174 68 150 68H36Z"
FIO_V2 = "M36 224C36 112 78 32 150 32C198 32 228 68 228 116C228 164 196 200 150 200C112 200 86 174 86 140C86 106 112 82 146 82H224"


def desenho(caminho, traco):
    if traco:
        return f'<path fill="none" stroke="#000000" stroke-width="28" stroke-linecap="round" stroke-linejoin="round" d="{caminho}"/>'
    return f'<path fill="#000000" fill-rule="evenodd" d="{caminho}"/>'


def svg_symbol(titulo, caminho, traco=False):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" role="img" aria-labelledby="title"><title id="title">{titulo}</title>{desenho(caminho, traco)}</svg>'''


def svg_lockup(titulo, simbolo, modo, traco=False):
    if modo == "fonte":
        arte = f'<g transform="translate(250 32) scale(.62)">{desenho(simbolo, traco)}</g><path fill="#000000" d="{SCLARI}" transform="scale(4)"/><circle cx="921.4" cy="44.3" r="15.4" fill="#000000"/>'
    else:
        arte = f'<g transform="translate(10 10) scale(.92)">{desenho(simbolo, traco)}</g><path fill="#000000" d="{PALAVRA}" transform="translate(14 0) scale(4)"/><circle cx="923.4" cy="44.3" r="15.4" fill="#000000"/>'
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 256" role="img" aria-labelledby="title"><title id="title">{titulo}</title>{arte}</svg>'''


def gravar(slug, versao, caminho, modo, titulo, traco=False):
    (DESTINO / f"{slug}-symbol-{versao}.svg").write_text(svg_symbol(f"{titulo}, símbolo {versao}", caminho, traco), encoding="utf-8")
    (DESTINO / f"{slug}-lockup-{versao}.svg").write_text(svg_lockup(f"Esclari, {titulo}, lockup {versao}", caminho, modo, traco), encoding="utf-8")


def finalizar(slug):
    for parte in ["symbol", "lockup"]:
        origem = DESTINO / f"{slug}-{parte}-v2.svg"
        conteudo = origem.read_text(encoding="utf-8").replace(", símbolo v2", "").replace(", lockup v2", "")
        (DESTINO / f"{slug}-{parte}.svg").write_text(conteudo, encoding="utf-8")


gravar("fonte-embutida", "v1", FONTE_V1, "fonte", "Fonte embutida")
gravar("fonte-embutida", "v2", FONTE_V2, "fonte", "Fonte embutida")
gravar("manicula", "v1", MANICULA_V1, "simbolo", "Manícula contemporânea")
gravar("manicula", "v2", MANICULA_V2, "simbolo", "Manícula contemporânea")
gravar("fio-procedencia", "v1", FIO_V1, "simbolo", "Fio de procedência")
gravar("fio-procedencia", "v2", FIO_V2, "simbolo", "Fio de procedência", True)
for nome in ["fonte-embutida", "manicula", "fio-procedencia"]:
    finalizar(nome)
