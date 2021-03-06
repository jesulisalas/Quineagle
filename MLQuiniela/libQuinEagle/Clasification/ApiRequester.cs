﻿using System;
using System.Net;
using System.Linq;
using System.Reflection;
using log4net;
using MMLib.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace libQuinEagle.Clasification
{
	/// <summary>
	/// Obtiene una clasificacion dada usando la API de football-data.org
	/// </summary>
	public class ApiRequester
	{
		private const string FOLDERPATH = @"leagues";

		private ILog Log { get; } = LogManager.GetLogger( MethodBase.GetCurrentMethod().DeclaringType );

		public string RequestHeader { get; set; }

		public string API_KEY { get; set; }
        
		public Dictionary<string, string> LeagueRequest { get; set; }

		public string API_URL { get; set; }

		//private Dictionary<LeagueEnum, LeagueTable> _leagues = new Dictionary<LeagueEnum, LeagueTable>();
		private Dictionary<LeagueEnum, Dictionary<int, LeagueTable>> _leagues = new Dictionary<LeagueEnum, Dictionary<int, LeagueTable>>()
		{
			{ LeagueEnum.PRIMERA , new Dictionary<int,LeagueTable>() },
			{ LeagueEnum.SEGUNDA, new Dictionary<int, LeagueTable>() }
		};

		private void _downloadLeague( LeagueEnum league, int journey )
		{
			//http://api.football-data.org/v1/competitions/436/leagueTable/?matchday=22"
			LeagueTable table = null;

			Log.Debug( $"Solicitando datos de la liga {EnumUtility.GetDescriptionFromEnumValue(league)}" );

			try
			{
				//string request = $"{API_URL}{LeagueRequest}";
				string leaguerequest = null;
				LeagueRequest.TryGetValue( EnumUtility.GetDescriptionFromEnumValue( league ), out leaguerequest);

				string request = $"{API_URL}{leaguerequest}".Replace("$JOURNEY$", journey.ToString()) ;

				//request = request.Replace( "$LEAGUEID$", ( ( int )league ).ToString() );

				WebHeaderCollection headers = new WebHeaderCollection();
				headers.Add( RequestHeader, API_KEY );

                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                wc.Headers = headers;

                Log.Debug( $"Request: {request}" );
            	var json = wc.DownloadString( request );
				table = JsonConvert.DeserializeObject<LeagueTable>( json );

				// convierto los nombres a mayusculas y sin acentos
				table.standing.Select( a => { a.teamName = a.teamName.RemoveDiacritics().ToUpper(); return a; } ).ToList();
				// Por cada nombre en la tabla, lo busco en la TeamsNames y lo sustituyo si lo encuentra
				foreach( var team in table.standing )
				{
					team.teamName = Teams.GetKeyfromName( team.teamName );
				}
			}
			catch( Exception e )
			{
				Log.Warn( $"Hubo un error al solicitar la clasificacion {EnumUtility.GetDescriptionFromEnumValue( league )}" );
				Log.Warn( e.Message );
			}

			if( table != null )
			{
				var fichero = $"{FOLDERPATH}/2017_{EnumUtility.GetDescriptionFromEnumValue( league )}_{journey}.json";
				Log.Info( $"Salvando {journey} de la liga {EnumUtility.GetDescriptionFromEnumValue( league )} en fichero {fichero}" );
				File.WriteAllText( fichero, JsonConvert.SerializeObject( table, Formatting.Indented ) );
			}

			_leagues[ league ][journey] = table;
		}

		private void _readLeagueFromFile( LeagueEnum league, int journey )
		{
			var fichero = $"{FOLDERPATH}/2017_{EnumUtility.GetDescriptionFromEnumValue( league )}_{journey}.json";
			LeagueTable table = null;

			try
			{
				table = JsonConvert.DeserializeObject<LeagueTable>( File.ReadAllText( fichero ) );
			}
			catch( Exception e )
			{
				Log.Error( $"No al leer el fichero '{fichero}': {e.Message}" );
			}

			_leagues[ league ][ journey ] = table;
		}
		

		public LeagueTable GetLeague( LeagueEnum league, int journey )
		{
			// Busca en diccionario, despues en ficheros y si no lo descarga
			LeagueTable tabla = null;

			if (!_leagues[league].TryGetValue( journey, out tabla ) )
			{
				Log.Info( $"No existe en memoria la jornada {journey} de la liga {EnumUtility.GetDescriptionFromEnumValue(league)}" );

				// Comprobar si existe el directorio con los ficheros csv
				if( !Directory.Exists( FOLDERPATH ) )
				{
					Directory.CreateDirectory( FOLDERPATH );
				}

				if( !File.Exists( $"{FOLDERPATH}/2017_{EnumUtility.GetDescriptionFromEnumValue( league )}_{journey}.json"))
				{
					Log.Info( $"Descargando jornada {journey} de la liga {EnumUtility.GetDescriptionFromEnumValue(league)}" );
					_downloadLeague( league, journey );
				}
				else
				{
					Log.Info( $"leyendo de fichero la jornada {journey} de la liga {EnumUtility.GetDescriptionFromEnumValue(league)}" );
					_readLeagueFromFile( league, journey );
				} 	
			}

			tabla = _leagues[league][journey];

			return tabla;
		}

		public LeagueEnum GetDivision(string team)
		{
			LeagueEnum res = LeagueEnum.PRIMERA;

			LeagueTable lt = GetLeague(LeagueEnum.PRIMERA, 1);

			if (lt.standing.FirstOrDefault(a => a.teamName == team) == null)
				res = LeagueEnum.SEGUNDA;

			return res;
		}
			   

		public void PrintLeague( LeagueEnum league, int journey )
		{
			LeagueTable liga = GetLeague(league,journey);

			Log.Info( $"Jornada numero {liga.matchday}" );
			Log.Info( $"POS - NOMBRE - PUNTOS - JUGADOS - GANADOS - PERDIDOS - EMPATADOS - G.FAVOR - G.CONTRA" );

			foreach (var c in liga.standing)
			{
				Log.Info($"{c.position} - {c.teamName} - {c.points} - {c.playedGames} - {c.wins} - {c.losses} - {c.draws} - {c.goals} - {c.goalsAgainst}");
			}
		}

	}
}
