namespace Esclari.Api.Tests;

public class EmailSaidaDominioTests
{
    static readonly DateTime Agora = new(2026, 11, 12, 12, 0, 0, DateTimeKind.Utc);

    static EmailSaida ComFalhas(int falhas)
    {
        var e = EmailSaida.Criar("aluno@teste.esclari", "Aviso", "Olá", Agora);
        for (var n = 0; n < falhas; n++) e.RegistrarFalha(Agora);
        return e;
    }

    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(1, 2, 4)]
    [InlineData(4, 5, 32)]
    public void Registrar_falha_soma_uma_tentativa_e_agenda_2_elevado_a_tentativas_minutos(int antes, int depois, int minutos)
    {
        var e = ComFalhas(antes);
        Assert.Equal(antes, e.Tentativas);

        e.RegistrarFalha(Agora);

        Assert.Equal((depois, Agora.AddMinutes(minutos)), (e.Tentativas, e.ProximaTentativa));
    }

    [Fact]
    public void Criar_agenda_para_agora_sem_tentativas_e_sem_envio()
    {
        var e = EmailSaida.Criar("aluno@teste.esclari", "Aviso", "Olá", Agora);

        Assert.Equal(("aluno@teste.esclari", "Aviso", "Olá", 0, Agora, (DateTime?)null), (e.Destinatario, e.Assunto, e.Corpo, e.Tentativas, e.ProximaTentativa, e.EnviadoEm));
    }

    [Fact]
    public void Concluir_marca_o_envio()
    {
        var e = ComFalhas(1);

        e.Concluir(Agora.AddMinutes(3));

        Assert.Equal(Agora.AddMinutes(3), e.EnviadoEm);
    }

    [Fact]
    public void Criar_sem_titulo_e_acao_deixa_nulos()
    {
        var e = EmailSaida.Criar("aluno@teste.esclari", "Aviso", "Olá", Agora);

        Assert.Equal((null, null, null), (e.Titulo, e.AcaoRotulo, e.AcaoUrl));
    }

    [Fact]
    public void Criar_com_titulo_e_acao_guarda_os_tres_campos()
    {
        var e = EmailSaida.Criar("aluno@teste.esclari", "Aviso", "Olá", Agora, new("T", new("Abrir", "http://x/y")));

        Assert.Equal(("T", "Abrir", "http://x/y"), (e.Titulo, e.AcaoRotulo, e.AcaoUrl));
    }}
