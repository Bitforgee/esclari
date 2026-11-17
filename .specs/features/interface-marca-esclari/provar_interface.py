import struct
import subprocess
import sys
import unittest
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[3]
FEATURE = Path(__file__).resolve().parent
CAPTURAS = FEATURE / "capturas"
BASE = "49cba64"
TELAS = ["login", "assistente", "solicitacoes", "chamado", "fila", "indicadores", "aparencia"]
TEMAS = ["claro", "escuro"]
TAMANHOS = {360: (360, 780), 1280: (1280, 800)}
TESTES_PERMITIDOS = {
    "web/src/test/interface.test.ts",
    "web/src/test/icones.test.ts",
    "web/src/guidelinesGlobal.test.ts",
    "web/src/aparencia.test.tsx",
    "web/src/administracao/aparencia/guidelinesCores.test.tsx",
    "web/e2e/interface-marca.spec.ts",
    "Esclari.Api.Tests/ManifestoTests.cs",
}


def dimensoes(caminho):
    dados = caminho.read_bytes()[:24]
    if dados[:8] != b"\x89PNG\r\n\x1a\n":
        raise AssertionError(f"{caminho.name} não é PNG")
    return struct.unpack(">II", dados[16:24])


def eh_teste(caminho):
    nome = caminho.split("/")[-1]
    return caminho.startswith("web/e2e/") or ".test." in nome or caminho.startswith("Esclari.Api.Tests/")


class InterfaceMarcaTests(unittest.TestCase):
    def test_C18_capturas(self):
        esperados = {f"{tela}-{tema}-{largura}.png": tamanho for tela in TELAS for tema in TEMAS for largura, tamanho in TAMANHOS.items()}
        self.assertEqual(len(esperados), 28)
        presentes = {p.name for p in CAPTURAS.glob("*.png")}
        self.assertEqual(sorted(presentes), sorted(esperados))
        for nome, tamanho in esperados.items():
            self.assertEqual(dimensoes(CAPTURAS / nome), tamanho, nome)

    def test_C19_testes_tocados(self):
        saida = subprocess.run(["git", "diff", "--name-only", f"{BASE}..HEAD", "--", "web", "Esclari.Api.Tests"], cwd=RAIZ, capture_output=True, text=True, check=True).stdout
        tocados = {linha.strip() for linha in saida.splitlines() if linha.strip() and eh_teste(linha.strip())}
        self.assertTrue(tocados, "nenhum teste tocado: a prova não viu o diff")
        self.assertEqual(sorted(tocados - TESTES_PERMITIDOS), [])


if __name__ == "__main__":
    unittest.main(argv=[sys.argv[0], *sys.argv[1:]])
