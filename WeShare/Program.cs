using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace WebAPI
{
	public class Program
	{
		public static void Main(string[] args)
		{

			var builder = WebApplication.CreateBuilder(args);
			
			// Add services to the container.
			
			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			builder.Services.AddDbContext<WeshareContext>(options =>
			{
			    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
			});
			
			builder.Services.AddControllers().AddJsonOptions(options =>
			{
			    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
			});

			builder.Services.AddCors(policyBuilder =>
			policyBuilder.AddDefaultPolicy(policy =>
				{
				policy.AllowAnyOrigin();
				policy.AllowAnyHeader();
				policy.AllowAnyMethod();
				}
				)
			);

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			//app.UseHttpsRedirection();

			app.UseCors();
			app.UseAuthorization();

			app.MapControllers();

			app.Run();

		}
	}
}