using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using TrilhaNetAzureDesafio.Context;
using TrilhaNetAzureDesafio.Models;

namespace TrilhaNetAzureDesafio.Controllers;

[ApiController]
[Route("[controller]")]
public class FuncionarioController : ControllerBase
{
    private readonly RHContext _context;
    private readonly string _connectionString;
    private readonly string _tableName;

    public FuncionarioController(RHContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetValue<string>("ConnectionStrings:SAConnectionString");
        _tableName = configuration.GetValue<string>("ConnectionStrings:AzureTableName");
    }

    private TableClient GetTableClient()
    {
        var serviceClient = new TableServiceClient(_connectionString);
        var tableClient = serviceClient.GetTableClient(_tableName);

        tableClient.CreateIfNotExists();
        return tableClient;
    }

    [HttpGet("{id}")]
    public IActionResult ObterPorId(int id)
    {
        if (id <= 0)
            return BadRequest("O ID fornecido é inválido.");

        try
        {
            var funcionario = _context.Funcionarios.Find(id);

            if (funcionario == null)
                return NotFound("Funcionário não encontrado.");

            return Ok(funcionario);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Erro ao obter os dados do funcionário: " + ex.Message);
        }
    }

    [HttpPost]
    public IActionResult Criar(Funcionario funcionario)
    {
        if (funcionario == null)
            return BadRequest("Os dados do funcionário são obrigatórios.");

        if (string.IsNullOrWhiteSpace(funcionario.Nome))
            return BadRequest("O nome do funcionário é obrigatório.");

        if (string.IsNullOrWhiteSpace(funcionario.EmailProfissional))
            return BadRequest("O e-mail profissional é obrigatório.");

        if (funcionario.Salario <= 0)
            return BadRequest("O salário deve ser maior que zero.");

        _context.Funcionarios.Add(funcionario);
        // TODO: Chamar o método SaveChanges do _context para salvar no Banco SQL
        try
        {
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Erro ao salvar os dados no banco de dados: " + ex.Message);
        }

        var tableClient = GetTableClient();
        var funcionarioLog = new FuncionarioLog(funcionario, TipoAcao.Inclusao, funcionario.Departamento, Guid.NewGuid().ToString());

        // TODO: Chamar o método UpsertEntity para salvar no Azure Table
        try
        {
            tableClient.UpsertEntity(funcionarioLog);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Erro ao salvar o log no Azure Table: " + ex.Message);
        }

        return CreatedAtAction(nameof(ObterPorId), new { id = funcionario.Id }, funcionario);
    }

    [HttpPut("{id}")]
    public IActionResult Atualizar(int id, Funcionario funcionario)
    {
        if (id <= 0)
            return BadRequest("O ID fornecido é inválido.");

        if (funcionario == null)
            return BadRequest("Os dados do funcionário são obrigatórios.");

        if (string.IsNullOrWhiteSpace(funcionario.Nome))
            return BadRequest("O nome do funcionário é obrigatório.");

        if (string.IsNullOrWhiteSpace(funcionario.EmailProfissional))
            return BadRequest("O e-mail profissional é obrigatório.");

        if (funcionario.Salario <= 0)
            return BadRequest("O salário deve ser maior que zero.");

        var funcionarioBanco = _context.Funcionarios.Find(id);

        if (funcionarioBanco == null)
            return NotFound("Funcionário não encontrado.");

        funcionarioBanco.Nome = funcionario.Nome;
        funcionarioBanco.Endereco = funcionario.Endereco;
        // TODO: As propriedades estão incompletas
        funcionarioBanco.Ramal = funcionario.Ramal;
        funcionarioBanco.EmailProfissional = funcionario.EmailProfissional;
        funcionarioBanco.Departamento = funcionario.Departamento;
        funcionarioBanco.Salario = funcionario.Salario;

        // TODO: Chamar o método de Update do _context.Funcionarios para salvar no Banco SQL
        try
        {
            _context.Funcionarios.Update(funcionarioBanco);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Erro ao atualizar os dados no banco de dados: " + ex.Message);
        }

        var tableClient = GetTableClient();
        var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Atualizacao, funcionarioBanco.Departamento, Guid.NewGuid().ToString());

        // TODO: Chamar o método UpsertEntity para salvar no Azure Table
        try
        {
            tableClient.UpsertEntity(funcionarioLog);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Erro ao salvar o log no Azure Table: " + ex.Message);
        }

        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult Deletar(int id)
    {
        if (id <= 0)
            return BadRequest("O ID fornecido é inválido.");

        var funcionarioBanco = _context.Funcionarios.Find(id);

        if (funcionarioBanco == null)
            return NotFound("Funcionário não encontrado.");

        // TODO: Chamar o método de Remove do _context.Funcionarios para salvar no Banco SQL
        try
        {
            _context.Funcionarios.Remove(funcionarioBanco);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Erro ao deletar os dados no banco de dados: " + ex.Message);
        }

        var tableClient = GetTableClient();
        var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Remocao, funcionarioBanco.Departamento, Guid.NewGuid().ToString());

        // TODO: Chamar o método UpsertEntity para salvar no Azure Table
        try
        {
            tableClient.UpsertEntity(funcionarioLog);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Erro ao salvar o log no Azure Table: " + ex.Message);
        }

        return NoContent();
    }
}
