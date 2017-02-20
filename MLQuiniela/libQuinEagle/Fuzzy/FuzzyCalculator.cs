﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Accord.Fuzzy;
using log4net;

namespace libQuinEagle.Fuzzy
{
	public class FuzzyCalculator
	{
		private ILog Log { get; } = LogManager.GetLogger( MethodBase.GetCurrentMethod().DeclaringType );

		private LinguisticVariable _lvlProbabilidad = new LinguisticVariable( "Probabilidad", 0f, 100f );

		private FuzzySet _fsDos = null;
		private FuzzySet _fsEquis = null;
		private FuzzySet _fsUno = null;

		private bool IsSet = false;

		public int MaxMultipleBets { get; set; } = 3;

		public void SetFuzzyValues( float f1_1, float f2_1, float f1_X, float f2_X, float f3_X, float f4_X, float f1_2, float f2_2 )
		{
			_lvlProbabilidad.ClearLabels();

			_fsUno = new FuzzySet( "UNO", new TrapezoidalFunction( f1_1, f2_1, TrapezoidalFunction.EdgeType.Left ) );
			_fsEquis = new FuzzySet( "EQUIS", new TrapezoidalFunction( f1_X, f2_X, f3_X, f4_X ) );
			_fsDos = new FuzzySet( "DOS", new TrapezoidalFunction( f1_2, f2_2, TrapezoidalFunction.EdgeType.Right) );

			Log.Info( $"Setting Function for '2' with values 0 - {f1_2} - {f2_2}" );
			Log.Info( $"Setting Function for 'X' with values {f1_X} - {f2_X} - {f3_X} - {f4_X}" );
			Log.Info( $"Setting Function for '1' with values {f1_1} - {f1_2} - 100" );

			_lvlProbabilidad.AddLabel( _fsUno );
			_lvlProbabilidad.AddLabel( _fsDos );
			_lvlProbabilidad.AddLabel( _fsEquis );

			IsSet = true;
		}

		public void GetBet( ref List<Fixture> fixtures )
		{
			if( IsSet )
			{
				foreach( var f in fixtures )
				{
					f.Result = _getBet( f.Probability );
				}

				// Ahora recorremos la lista otra vez y cogemos las apuestas dobles
				// Eliminamos las que esten en los extremos hasta tener solo el numero de doblrd que queremos
				IEnumerable<Fixture> dobles = fixtures.Where( a => a.Result == QuinielaResult.ONEX || a.Result == QuinielaResult.TWOX );
				if( dobles.Count() > MaxMultipleBets )
				{
					
				}
			}
			else
				Log.Warn( "Valores difusos no establecidos" );
		}

		private QuinielaResult _getBet( float probability )
		{
			/*
			VOID 	= 0,
			ONE 	= 1,
			X 		= 2,
			TWO 	= 4,
			ONEX 	= 3,
			TWOX 	= 6,
			ONEXTWO = 7
			*/

			QuinielaResult res = QuinielaResult.VOID;

			float y1 = _lvlProbabilidad.GetLabelMembership( "UNO", probability );
			float yX = _lvlProbabilidad.GetLabelMembership( "EQUIS", probability );
			float y2 = _lvlProbabilidad.GetLabelMembership( "DOS", probability );

			if( y1 > 0.0 )
				res = ( QuinielaResult )( ( int )res + ( int )QuinielaResult.ONE );

			if( yX > 0.0 )
				res = (QuinielaResult ) (( int )res + ( int )QuinielaResult.X);

			if( y2 > 0) 
				res = ( QuinielaResult )( ( int )res + ( int )QuinielaResult.TWO );

			return res;
		}
	}
}