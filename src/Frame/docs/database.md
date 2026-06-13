## Процедура восстановления базы из Backup
- Команда копирования файла резервной копии dump_file.sql в корень контейнера с именем postgres14:
```
docker cp dump_file.sql postgres14:/dump_file.sql
```
- БД должна быть создана заранее
- Команда восстановления в заранее созданную пустую БД с именем vsp_prod из резервной копии с именем dump_file.sql в контейнере postgres14:
```
docker exec -it postgres14 bash
psql -U postgres -d vsp_prod < /dump_file.sql
```
