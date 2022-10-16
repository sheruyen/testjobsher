# Тестовое задание dotNet
Описание методов API и работы веб-сервиса.

### **Содержимое репозитория**
- Папка .Net проекта **RatesWebService**
- SQL файлы запросов (они также приведены ниже)
- Этот README

## RatesWebService — .Net Core проект (webapi)

В силу того, что проект маленький, решил реализовать весь функционал в одном контроллере `RateController`. Порты и стринг подключения к БД задаются в `appsettings.json`. В текущей версии оставил свои локальные настройки для наглядности. Все обращения в БД выполняются через сикл-процедуры.

### **Описание методов в веб-сервисе**

Во всех случаях параметр date принимает дату в формате `dd.MM.yyyy`

1. `GET .../currency/save/{date}`

Запрошенный функционал полностью реализован. Не вижу смысла дублировать текст из задания. Строки добавляются в БД по одному, поэтому количество считаю вручную в интеджере `rowsAffectedCount`. Работу с API нацбанка вынес в отдельный метод. Для работы с XML использовал `System.Xml.Linq`.

```csharp
public ActionResult<Rate> GetRatesAndUpdateDB(String date)

private async Task<String> GetRatesByDateFromNB(DateTime date)
```

2. `GET .../currency/{date}/{\*code}`

Тоже работает как указано в задании. Параметру code по умолчанию задаю значение NULL. Оба параметра всегда отправляются в БД-процедуру spGetRates, а там уже реализована проверка на NULL.
Возвращенную из БД таблицу сериализую спомощью `Dictionary` и возвращаю JSON.

```csharp
public ActionResult<Rate> GetTable(String date, String? code = null)
```

---

## SQL команды использованные для выполнения задания

```sql
--table creation command 
CREATE TABLE R_CURRENCY (
    ID int IDENTITY(1,1) PRIMARY KEY,
    TITLE varchar(60) NOT NULL,
    CODE varchar(3) NOT NULL,
    VALUE numeric(18,2) NOT NULL,
    A_DATE date NOT NULL
);

--procedure from the task
CREATE PROCEDURE dbo.spGetRates
	@A_DATE DATE,
	@CODE VARCHAR(3) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	if (@CODE IS NULL)
		BEGIN

			SELECT *
			FROM dbo.R_CURRENCY
			WHERE A_DATE = @A_DATE

		END
	else
		BEGIN

			SELECT *
			FROM dbo.R_CURRENCY
			WHERE A_DATE = @A_DATE AND CODE = @CODE

		END

END

--procedure used to query table data
ALTER PROCEDURE dbo.spUpdateRates
	@TITLE varchar(60),
	@CODE varchar(3),
	@VALUE numeric(18,2),
	@DATE date
AS
BEGIN
	SET NOCOUNT OFF;
	
	IF NOT EXISTS (SELECT * FROM dbo.R_CURRENCY WHERE CODE = @CODE AND A_DATE = @DATE)
	BEGIN
		INSERT INTO R_CURRENCY(TITLE, CODE, VALUE, A_DATE)
		VALUES (@TITLE, @CODE, @VALUE, @DATE);
	END
	
END
GO
```