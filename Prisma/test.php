<?php
$serverVariables = $_SERVER;
ksort( $serverVariables, SORT_NATURAL );

header( 'X-Test-HEADER: Test header' );
?><html>
	<head>
		<title>PHP's $_SERVER</title>
	</head>
	<body>
		<pre><?= json_encode( $serverVariables, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_LINE_TERMINATORS | JSON_UNESCAPED_UNICODE ); ?></pre>
		<pre><?= PHP_SAPI; ?></pre>
		<pre><?= sys_get_temp_dir(); ?></pre>
	</body>
</html>
